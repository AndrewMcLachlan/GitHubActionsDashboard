import { Outlet } from '@tanstack/react-router'
import { TanStackRouterDevtools } from '@tanstack/react-router-devtools'
import { Layout } from './layout/Layout'

function App() {

  return (
    <Layout>
      <Outlet />
      <TanStackRouterDevtools />
    </Layout>
  )
}

export default App
