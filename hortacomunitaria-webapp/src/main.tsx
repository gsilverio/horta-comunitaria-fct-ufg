import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'
import App from './App.tsx'
import { ConfirmHost } from './components/ConfirmDialog'
import { AuthProvider } from './features/auth/components/AuthProvider'

const root = document.getElementById('root')
if (!root) throw new Error('Elemento raiz da aplicação não encontrado.')
createRoot(root).render(
  <StrictMode>
    <AuthProvider><App /></AuthProvider>
    <ConfirmHost />
  </StrictMode>,
)
