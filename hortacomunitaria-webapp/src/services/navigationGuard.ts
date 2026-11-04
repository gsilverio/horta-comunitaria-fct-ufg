let guard: (() => boolean) | undefined
export function registerNavigationGuard(value: () => boolean) { guard = value; return () => { if (guard === value) guard = undefined } }
export function releaseNavigationGuard() { guard = undefined }
export function authorizeNavigation() { if (!guard || guard()) { guard = undefined; return true }; return false }
