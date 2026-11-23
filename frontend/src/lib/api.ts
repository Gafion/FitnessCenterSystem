import createClient from 'openapi-fetch';
import type { components, paths } from '@/api/schema';

// The only way the frontend calls the API (rule 6). Types come from the generated
// schema; run `bun run api:generate` after the backend's OpenAPI document changes.
export const api = createClient<paths>();

export type Schemas = components['schemas'];

/** Validation errors by field name, as the API returns them; "" holds errors for no field. */
export type FieldErrors = Record<string, string[]>;
