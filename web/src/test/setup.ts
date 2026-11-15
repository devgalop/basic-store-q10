// Matchers de DOM (toBeInTheDocument, toHaveValue...). El desmontaje entre tests lo hace Testing Library solo,
// porque `globals: true` (vite.config.ts) le deja registrar su afterEach(cleanup) en cada archivo de test.
import '@testing-library/jest-dom/vitest';
