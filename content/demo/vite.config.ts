import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { execSync } from 'node:child_process';

// Try to determine the base URL from the git repository
// If this doesn't work for you, don't hesite to remove this function and hardcode the base URL
// in the `defineConfig` function below
function findBaseUrlFromRemoteUrl() {
    const remoteUrl = execSync("git remote get-url origin").toString();

    const findRemoteBaseUrl = /git@github\.com:.*\/(.*).git/gm;

    const match = findRemoteBaseUrl.exec(remoteUrl);

    if (match) {
        return match[1];
    }

    throw new Error(`Could not determine base URL from remote URL, please make sure you are in a git repository and have remote origin set`);
}

// https://vitejs.dev/config/
export default defineConfig(async ({ command, mode }) => {
    // Try to determine the base URL
    let baseUrl = '/';

    if (command === 'build') {
        baseUrl = findBaseUrlFromRemoteUrl();
    }

    return {
        base: baseUrl,
        plugins: [
            // If you need React, uncomment the following line
            // react({
            //     jsxRuntime: 'classic',
            // })
        ],
        server: {
            watch: {
                ignored: [
                    "**/*.fs"
                ]
            }
        },
        clearScreen: false,
    }
})
