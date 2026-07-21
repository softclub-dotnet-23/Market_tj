import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { CartProvider } from "@/context/CartContext";
import { FavoritesProvider } from "@/context/FavoritesContext";
import { RootLayout } from "@/components/layout/RootLayout";
import { AppChrome } from "@/components/layout/AppChrome";
import { AdminLayout } from "@/components/layout/AdminLayout";
import { PageLoader } from "@/components/layout/PageLoader";
import { Home } from "@/pages/Home";

const Catalog = lazy(() => import("@/pages/Catalog").then((m) => ({ default: m.Catalog })));
const ProductDetails = lazy(() => import("@/pages/ProductDetails").then((m) => ({ default: m.ProductDetails })));
const About = lazy(() => import("@/pages/About").then((m) => ({ default: m.About })));
const Contact = lazy(() => import("@/pages/Contact").then((m) => ({ default: m.Contact })));
const Login = lazy(() => import("@/pages/Login").then((m) => ({ default: m.Login })));
const Register = lazy(() => import("@/pages/Register").then((m) => ({ default: m.Register })));
const Forbidden = lazy(() => import("@/pages/Forbidden").then((m) => ({ default: m.Forbidden })));
const NotFound = lazy(() => import("@/pages/NotFound").then((m) => ({ default: m.NotFound })));
const AdminDashboard = lazy(() => import("@/pages/AdminDashboard").then((m) => ({ default: m.AdminDashboard })));

function App() {
  return (
    <BrowserRouter>
      <CartProvider>
        <FavoritesProvider>
          <Suspense fallback={<PageLoader />}>
            <Routes>
              <Route element={<AppChrome />}>
                <Route path="login" element={<Login />} />
                <Route path="register" element={<Register />} />

                <Route path="admin" element={<AdminLayout />}>
                  <Route index element={<AdminDashboard />} />
                  <Route path="*" element={<Navigate to="/admin" replace />} />
                </Route>

                <Route element={<RootLayout />}>
                  <Route index element={<Home />} />
                  <Route path="catalog" element={<Catalog />} />
                  <Route path="product/:slug" element={<ProductDetails />} />
                  <Route path="about" element={<About />} />
                  <Route path="contact" element={<Contact />} />
                  <Route path="forbidden" element={<Forbidden />} />
                  <Route path="*" element={<NotFound />} />
                </Route>
              </Route>
            </Routes>
          </Suspense>
        </FavoritesProvider>
      </CartProvider>
    </BrowserRouter>
  );
}

export default App;
