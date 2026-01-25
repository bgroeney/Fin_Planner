#!/bin/bash
# FinPlanner GCP Deployment Script
# Deploys API to Cloud Run and Frontend to Firebase Hosting

set -e

# Configuration - EDIT THESE
PROJECT_ID="${GCP_PROJECT_ID:-}"
REGION="${GCP_REGION:-australia-southeast1}"
SERVICE_NAME="finplanner-api"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log() { echo -e "${GREEN}[DEPLOY]${NC} $1"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
error() { echo -e "${RED}[ERROR]${NC} $1"; exit 1; }

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# Check prerequisites
check_prerequisites() {
    log "Checking prerequisites..."
    
    command -v gcloud &> /dev/null || error "gcloud CLI not installed. Install from: https://cloud.google.com/sdk/docs/install"
    command -v firebase &> /dev/null || warn "Firebase CLI not installed. Install with: npm install -g firebase-tools"
    command -v dotnet &> /dev/null || error "dotnet CLI not installed"
    command -v npm &> /dev/null || error "npm not installed"
    
    if [ -z "$PROJECT_ID" ]; then
        echo ""
        echo "Please set your GCP Project ID:"
        read -p "GCP_PROJECT_ID: " PROJECT_ID
        [ -z "$PROJECT_ID" ] && error "Project ID is required"
    fi
    
    log "Using project: $PROJECT_ID"
}

# Setup GCP project
setup_gcp() {
    log "Setting up GCP project..."
    
    gcloud config set project "$PROJECT_ID"
    
    log "Enabling required APIs..."
    gcloud services enable \
        run.googleapis.com \
        secretmanager.googleapis.com \
        cloudbuild.googleapis.com \
        containerregistry.googleapis.com \
        artifactregistry.googleapis.com \
        2>/dev/null || true
}

# Create secrets
setup_secrets() {
    log "Setting up secrets..."
    
    # Check if secrets exist
    if ! gcloud secrets describe DATABASE_URL &>/dev/null; then
        echo ""
        echo "Enter your Neon PostgreSQL connection string:"
        echo "(Format: postgres://user:pass@host/dbname?sslmode=require)"
        read -s -p "DATABASE_URL: " DATABASE_URL
        echo ""
        [ -z "$DATABASE_URL" ] && error "DATABASE_URL is required"
        echo -n "$DATABASE_URL" | gcloud secrets create DATABASE_URL --data-file=-
        log "Created DATABASE_URL secret"
    else
        log "DATABASE_URL secret already exists"
    fi
    
    if ! gcloud secrets describe JWT_SECRET &>/dev/null; then
        # Generate random JWT secret
        JWT_SECRET=$(openssl rand -base64 32)
        echo -n "$JWT_SECRET" | gcloud secrets create JWT_SECRET --data-file=-
        log "Created JWT_SECRET secret (auto-generated)"
    else
        log "JWT_SECRET secret already exists"
    fi
    
    if ! gcloud secrets describe GEMINI_API_KEY &>/dev/null; then
        echo ""
        read -s -p "GEMINI_API_KEY (press Enter to skip): " GEMINI_KEY
        echo ""
        if [ -n "$GEMINI_KEY" ]; then
            echo -n "$GEMINI_KEY" | gcloud secrets create GEMINI_API_KEY --data-file=-
            log "Created GEMINI_API_KEY secret"
        else
            warn "Skipping GEMINI_API_KEY - AI features will be disabled"
        fi
    else
        log "GEMINI_API_KEY secret already exists"
    fi
}

# Build and deploy API
deploy_api() {
    log "Building and deploying API to Cloud Run..."
    
    cd "$PROJECT_DIR/Mineplex.FinPlanner.Api"
    
    # Build container image
    log "Building container image..."
    gcloud builds submit --tag "gcr.io/$PROJECT_ID/$SERVICE_NAME" .
    
    # Deploy to Cloud Run
    log "Deploying to Cloud Run..."
    
    # Build secrets argument
    SECRETS_ARG="ConnectionStrings__DefaultConnection=DATABASE_URL:latest"
    SECRETS_ARG="$SECRETS_ARG,Jwt__Key=JWT_SECRET:latest"
    
    if gcloud secrets describe GEMINI_API_KEY &>/dev/null; then
        SECRETS_ARG="$SECRETS_ARG,Gemini__ApiKey=GEMINI_API_KEY:latest"
    fi
    
    gcloud run deploy "$SERVICE_NAME" \
        --image "gcr.io/$PROJECT_ID/$SERVICE_NAME" \
        --region "$REGION" \
        --platform managed \
        --allow-unauthenticated \
        --min-instances 0 \
        --max-instances 5 \
        --memory 512Mi \
        --cpu 1 \
        --timeout 300 \
        --set-secrets "$SECRETS_ARG" \
        --set-env-vars "ASPNETCORE_ENVIRONMENT=Production"
    
    # Get the service URL
    API_URL=$(gcloud run services describe "$SERVICE_NAME" --region "$REGION" --format 'value(status.url)')
    log "API deployed to: $API_URL"
    
    echo "$API_URL" > "$SCRIPT_DIR/.api_url"
}

# Build and deploy frontend
deploy_frontend() {
    log "Building and deploying frontend to Firebase..."
    
    cd "$PROJECT_DIR/fin-planner-ui"
    
    # Get API URL
    if [ -f "$SCRIPT_DIR/.api_url" ]; then
        API_URL=$(cat "$SCRIPT_DIR/.api_url")
    else
        API_URL=$(gcloud run services describe "$SERVICE_NAME" --region "$REGION" --format 'value(status.url)' 2>/dev/null || echo "")
    fi
    
    if [ -z "$API_URL" ]; then
        error "API URL not found. Deploy API first."
    fi
    
    log "Using API URL: $API_URL"
    
    # Update .env.production
    echo "VITE_API_BASE_URL=$API_URL" > .env.production
    
    # Install dependencies and build
    log "Installing dependencies..."
    npm ci
    
    log "Building frontend..."
    npm run build
    
    # Initialize Firebase if needed
    if [ ! -f ".firebaserc" ]; then
        log "Initializing Firebase..."
        firebase use "$PROJECT_ID" --add 2>/dev/null || firebase login
        firebase use "$PROJECT_ID"
    fi
    
    # Deploy to Firebase Hosting
    log "Deploying to Firebase Hosting..."
    firebase deploy --only hosting --project "$PROJECT_ID"
    
    # Get hosting URL
    log "Frontend deployed!"
}

# Update CORS on Cloud Run
update_cors() {
    log "Updating CORS configuration..."
    
    # Get Firebase hosting URL
    FRONTEND_URL="https://$PROJECT_ID.web.app"
    
    # Redeploy with updated CORS
    cd "$PROJECT_DIR/Mineplex.FinPlanner.Api"
    
    gcloud run services update "$SERVICE_NAME" \
        --region "$REGION" \
        --set-env-vars "AllowedOrigins__0=$FRONTEND_URL,AllowedOrigins__1=https://$PROJECT_ID.firebaseapp.com"
    
    log "CORS updated for $FRONTEND_URL"
}

# Run database migrations
run_migrations() {
    log "Running database migrations..."
    
    # Get database URL from secret
    DATABASE_URL=$(gcloud secrets versions access latest --secret=DATABASE_URL)
    
    cd "$PROJECT_DIR/Mineplex.FinPlanner.Api"
    dotnet ef database update --connection "$DATABASE_URL"
    
    log "Migrations complete"
}

# Main deployment flow
main() {
    echo ""
    echo "╔═══════════════════════════════════════╗"
    echo "║   FinPlanner GCP Deployment Script    ║"
    echo "╚═══════════════════════════════════════╝"
    echo ""
    
    check_prerequisites
    setup_gcp
    setup_secrets
    
    echo ""
    echo "Ready to deploy. Select an option:"
    echo "  1) Full deployment (API + Frontend + Migrations)"
    echo "  2) Deploy API only"
    echo "  3) Deploy Frontend only"
    echo "  4) Run database migrations only"
    echo "  5) Update CORS settings"
    echo ""
    read -p "Option [1]: " OPTION
    OPTION=${OPTION:-1}
    
    case $OPTION in
        1)
            run_migrations
            deploy_api
            deploy_frontend
            update_cors
            ;;
        2)
            deploy_api
            ;;
        3)
            deploy_frontend
            ;;
        4)
            run_migrations
            ;;
        5)
            update_cors
            ;;
        *)
            error "Invalid option"
            ;;
    esac
    
    echo ""
    log "═══════════════════════════════════════"
    log "Deployment complete!"
    
    if [ -f "$SCRIPT_DIR/.api_url" ]; then
        echo ""
        echo "  API:      $(cat $SCRIPT_DIR/.api_url)"
        echo "  Frontend: https://$PROJECT_ID.web.app"
        echo "  Health:   $(cat $SCRIPT_DIR/.api_url)/health"
    fi
    
    log "═══════════════════════════════════════"
}

# Run main
main "$@"
