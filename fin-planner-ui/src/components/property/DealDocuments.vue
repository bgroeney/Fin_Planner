<template>
  <div class="deal-documents">
    <div class="docs-header flex-between mb-lg">
       <div class="flex gap-md items-center">
          <input type="file" ref="fileInput" @change="handleFileSelect" class="hidden" multiple />
          <button @click="$refs.fileInput.click()" class="btn btn-primary" :disabled="uploading">
             <span v-if="uploading" class="spinner-sm mr-sm"></span>
             <span v-else>+</span> Upload Document
          </button>
          <span v-if="uploading" class="text-sm text-muted">Uploading...</span>
       </div>
       <div class="search-box">
          <input v-model="searchQuery" placeholder="Search documents..." class="form-input-sm" />
       </div>
    </div>

    <div v-if="loading" class="flex-center p-xl">
        <MoneyBoxLoader size="lg" text="Loading Documents..." />
    </div>

    <div v-else-if="documents.length === 0" class="empty-state card flex-center flex-col p-xl">
        <div class="empty-icon-bg mb-md">
            <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>
        </div>
        <h3>No Documents Yet</h3>
        <p>Upload contracts, due diligence reports, or floor plans.</p>
    </div>

    <div v-else class="documents-grid">
        <div v-for="doc in filteredDocuments" :key="doc.id" class="doc-card card" @click="previewDocument(doc)">
            <div class="doc-icon">
                <svg v-if="doc.contentType?.includes('pdf')" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#ef4444" stroke-width="1.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="12" y1="18" x2="12" y2="12"/><line x1="9" y1="15" x2="15" y2="15"/></svg>
                <svg v-else-if="doc.contentType?.includes('image')" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 24 24" fill="none" stroke="#3b82f6" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>
                <svg v-else xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
            </div>
            <div class="doc-info">
                <div class="doc-name text-truncate" :title="doc.fileName">{{ doc.fileName }}</div>
                <div class="doc-meta">
                    {{ formatFileSize(doc.fileSizeBytes) }} • {{ formatDate(doc.uploadedAt) }}
                </div>
                <div class="doc-tags mt-xs" v-if="doc.tags">
                    <span v-for="tag in doc.tags.split(',')" :key="tag" class="tag-badge">{{ tag }}</span>
                </div>
            </div>
            <div class="doc-actions">
                 <button @click.stop="deleteDocument(doc.id)" class="btn-icon text-danger" title="Delete">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                 </button>
            </div>
        </div>
    </div>

    <!-- Preview Modal -->
    <Teleport to="body">
    <div v-if="showPreview" class="modal-backdrop" @click="closePreview">
        <div class="modal preview-modal card" @click.stop>
            <div class="modal-header flex-between">
                <div class="flex items-center gap-md">
                     <h3 class="m-0 text-truncate" style="max-width: 400px;">{{ selectedDoc?.fileName }}</h3>
                     <span class="tag-badge" v-if="selectedDoc?.tags">{{ selectedDoc.tags }}</span>
                </div>
                <div class="flex gap-sm">
                    <button @click="downloadFile(selectedDoc)" class="btn btn-secondary btn-sm" :disabled="previewLoading">Download</button>
                    <button @click="closePreview" class="btn-close">×</button>
                </div>
            </div>
            <div class="modal-body p-0 preview-body">
                <div v-if="previewLoading" class="flex-center h-full flex-col">
                    <span class="spinner-lg mb-md"></span>
                    <p>Loading document...</p>
                </div>
                <iframe v-else-if="previewUrl && selectedDoc.contentType === 'application/pdf'" :src="previewUrl" width="100%" height="100%" frameborder="0"></iframe>
                <img v-else-if="previewUrl && selectedDoc.contentType.includes('image')" :src="previewUrl" class="preview-image" />
                <div v-else class="flex-center h-full flex-col">
                    <p>Preview not available for this file type.</p>
                    <button @click="downloadFile(selectedDoc)" class="btn btn-primary mt-md">Download File</button>
                </div>
            </div>
        </div>
    </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import api from '../../services/api';
import MoneyBoxLoader from '../MoneyBoxLoader.vue';

const props = defineProps({
    dealId: { type: String, required: true }
});

const documents = ref([]);
const loading = ref(false);
const uploading = ref(false);
const searchQuery = ref('');
const fileInput = ref(null);

// Preview
const showPreview = ref(false);
const previewLoading = ref(false);
const selectedDoc = ref(null);
const previewUrl = ref(null);

const filteredDocuments = computed(() => {
    if(!searchQuery.value) return documents.value;
    const q = searchQuery.value.toLowerCase();
    return documents.value.filter(d => d.fileName.toLowerCase().includes(q) || d.tags?.toLowerCase().includes(q));
});

onMounted(() => {
    fetchDocuments();
});

const fetchDocuments = async () => {
    loading.value = true;
    try {
        const res = await api.get(`/propertydeals/${props.dealId}/documents`);
        documents.value = res.data;
    } catch (e) {
        console.error("Failed to load docs", e);
    } finally {
        loading.value = false;
    }
};

const handleFileSelect = async (e) => {
    const files = e.target.files;
    if (!files.length) return;
    
    uploading.value = true;
    try {
        for (let i = 0; i < files.length; i++) {
            const formData = new FormData();
            formData.append('file', files[i]);
            // Auto-tag based on name or allow edit? Simple for now.
            // formData.append('tags', 'Upload'); 
            
            await api.post(`/propertydeals/${props.dealId}/documents`, formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });
        }
        await fetchDocuments();
    } catch (e) {
        alert("Upload failed");
        console.error(e);
    } finally {
        uploading.value = false;
        fileInput.value.value = ''; // Reset
    }
};

const previewDocument = async (doc) => {
    selectedDoc.value = doc;
    showPreview.value = true;
    previewLoading.value = true;
    
    try {
        // Get blob for preview with inline=true to avoid attachment header
        const response = await api.get(`/propertydeals/${props.dealId}/documents/${doc.id}/download?inline=true`, { responseType: 'blob' });
        const blob = new Blob([response.data], { type: doc.contentType });
        previewUrl.value = URL.createObjectURL(blob);
    } catch (e) {
        console.error("Failed to load preview", e);
    } finally {
        previewLoading.value = false;
    }
};

const closePreview = () => {
    showPreview.value = false;
    previewLoading.value = false;
    if (previewUrl.value) URL.revokeObjectURL(previewUrl.value);
    previewUrl.value = null;
    selectedDoc.value = null;
};

const downloadFile = (doc) => {
     // Trigger browser download by creating anchor with the BLOB URL
     const link = document.createElement('a');
     link.href = previewUrl.value;
     link.download = doc.fileName;
     link.click();
};

const deleteDocument = async (id) => {
    if(!confirm("Delete this document?")) return;
    try {
        await api.delete(`/propertydeals/${props.dealId}/documents/${id}`);
        documents.value = documents.value.filter(d => d.id !== id);
    } catch (e) {
        alert("Delete failed");
    }
};

// Helpers
const formatFileSize = (bytes) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
};
const formatDate = (d) => new Date(d).toLocaleDateString();

</script>

<style scoped>
.documents-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: var(--spacing-md);
}
.doc-card {
    padding: var(--spacing-md);
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    cursor: pointer;
    transition: transform 0.2s, box-shadow 0.2s;
    position: relative;
    border: 1px solid var(--color-border);
}
.doc-card:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow-md);
    border-color: var(--color-industrial-copper);
}
.doc-icon {
    margin-bottom: var(--spacing-sm);
    color: var(--color-text-muted);
}
.doc-name {
    font-weight: 500;
    font-size: var(--font-size-sm);
    margin-bottom: 4px;
    width: 100%;
}
.doc-meta {
    font-size: 10px;
    color: var(--color-text-muted);
}
.tag-badge {
    background: var(--color-bg-elevated);
    border: 1px solid var(--color-border);
    border-radius: 4px;
    padding: 2px 6px;
    font-size: 10px;
    color: var(--color-text-secondary);
}
.doc-actions {
    position: absolute;
    top: 8px;
    right: 8px;
    opacity: 0;
    transition: opacity 0.2s;
}
.doc-card:hover .doc-actions {
    opacity: 1;
}
.btn-icon {
    background: rgba(255,255,255,0.8);
    border: none;
    border-radius: 4px;
    padding: 4px;
    cursor: pointer;
}
.btn-icon:hover {
    background: white;
    color: var(--color-danger);
}

.preview-modal {
    width: 900px;
    height: 90vh;
    display: flex;
    flex-direction: column;
}
.preview-body {
    flex: 1;
    background: #f3f4f6;
    overflow: hidden;
}
.preview-image {
    width: 100%;
    height: 100%;
    object-fit: contain;
}
.h-full { height: 100%; }
.flex-col { flex-direction: column; }
</style>
