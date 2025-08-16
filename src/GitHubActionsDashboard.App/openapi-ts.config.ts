import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
    input: 'http://localhost:5010/openapi/v1.json',
    output: './src/api',
    plugins: [
        {
            name: '@hey-api/client-axios',
            runtimeConfigPath: './src/utils/axios-config.ts',
        },
        '@tanstack/react-query'
    ],
});