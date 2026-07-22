import js from '@eslint/js'
import prettier from 'eslint-config-prettier'
import pluginVue from 'eslint-plugin-vue'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  {
    ignores: ['dist/**', 'node_modules/**', 'coverage/**', '*.tsbuildinfo'],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...pluginVue.configs['flat/essential'],
  {
    files: ['**/*.vue'],
    languageOptions: {
      parserOptions: {
        parser: tseslint.parser,
      },
    },
  },
  {
    files: ['src/components/drafts/visual/DraftVisualBoard.vue'],
    languageOptions: {
      globals: {
        AudioContext: 'readonly',
      },
    },
  },
  {
    files: ['src/views/DraftsView.vue'],
    languageOptions: {
      globals: {
        AbortController: 'readonly',
      },
    },
  },
  {
    files: [
      'src/components/users/DiscordLinkSection.vue',
      'src/views/LoginView.vue',
      'src/views/RegisterView.vue',
    ],
    languageOptions: {
      globals: {
        window: 'readonly',
      },
    },
  },
  {
    files: ['src/components/ui/**/*.vue'],
    rules: {
      'vue/multi-word-component-names': 'off',
    },
  },
  prettier,
)
