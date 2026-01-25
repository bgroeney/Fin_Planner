module.exports = {
    root: true,
    env: {
        browser: true,
        es2021: true,
        node: true
    },
    extends: [
        'eslint:recommended',
        'plugin:vue/vue3-recommended'
    ],
    parserOptions: {
        ecmaVersion: 'latest',
        sourceType: 'module'
    },
    rules: {
        // Relax some rules for pragmatic development
        'vue/multi-word-component-names': 'off',
        'vue/no-unused-vars': 'warn',
        'no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
        'no-console': ['warn', { allow: ['warn', 'error'] }],

        // Enforce consistent style
        'semi': ['error', 'always'],
        'quotes': ['error', 'single', { avoidEscape: true }],
        'comma-dangle': ['error', 'only-multiline'],

        // Vue specific  
        'vue/html-indent': ['error', 2],
        'vue/script-indent': ['error', 2, { baseIndent: 0 }],
        'vue/max-attributes-per-line': ['warn', { singleline: 3, multiline: 1 }],
        'vue/first-attribute-linebreak': 'off'
    }
};
