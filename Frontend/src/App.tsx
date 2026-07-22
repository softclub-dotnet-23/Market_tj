import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "@/context/AuthContext";
import { CartProvider } from "@/context/CartContext";
import { FavoritesProvider } from "@/context/FavoritesContext";
import { RootLayout } from "@/components/layout/RootLayout";
import { AppChrome } from "@/components/layout/AppChrome";
import { AdminLayout } from "@/components/layout/AdminLayout";
import { FarmerLayout } from "@/components/layout/FarmerLayout";
import { ProtectedRoute } from "@/components/layout/ProtectedRoute";
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
const AdminStatistics = lazy(() => import("@/pages/AdminStatistics").then((m) => ({ default: m.AdminStatistics })));
const AdminOrders = lazy(() => import("@/pages/AdminOrders").then((m) => ({ default: m.AdminOrders })));
const AdminProducts = lazy(() => import("@/pages/AdminProducts").then((m) => ({ default: m.AdminProducts })));
const AdminFarmers = lazy(() => import("@/pages/AdminFarmers").then((m) => ({ default: m.AdminFarmers })));
const AdminUsers = lazy(() => import("@/pages/AdminUsers").then((m) => ({ default: m.AdminUsers })));
const AdminCommissions = lazy(() => import("@/pages/AdminCommissions").then((m) => ({ default: m.AdminCommissions })));
const AdminReviews = lazy(() => import("@/pages/AdminReviews").then((m) => ({ default: m.AdminReviews })));
const AdminSettings = lazy(() => import("@/pages/AdminSettings").then((m) => ({ default: m.AdminSettings })));
const FarmerDashboard = lazy(() => import("@/pages/FarmerDashboard").then((m) => ({ default: m.FarmerDashboard })));
const FarmerProducts = lazy(() => import("@/pages/FarmerProducts").then((m) => ({ default: m.FarmerProducts })));

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <CartProvider>
          <FavoritesProvider>
            <Suspense fallback={<PageLoader />}>
              <Routes>
                <Route element={<AppChrome />}>
                  <Route path="login" element={<Login />} />
                  <Route path="register" element={<Register />} />

                  <Route element={<ProtectedRoute role="Admin" />}>
                    <Route path="admin" element={<AdminLayout />}>
                      <Route index element={<AdminDashboard />} />
                      <Route path="statistics" element={<AdminStatistics />} />
                      <Route path="orders" element={<AdminOrders />} />
                      <Route path="products" element={<AdminProducts />} />
                      <Route path="farmers" element={<AdminFarmers />} />
                      <Route path="users" element={<AdminUsers />} />
                      <Route path="commissions" element={<AdminCommissions />} />
                      <Route path="reviews" element={<AdminReviews />} />
                      <Route path="settings" element={<AdminSettings />} />
                      <Route path="*" element={<Navigate to="/admin" replace />} />
                    </Route>
                  </Route>

                  <Route element={<ProtectedRoute role="Farmer" />}>
                    <Route path="farmer" element={<FarmerLayout />}>
                      <Route index element={<FarmerDashboard />} />
                      <Route path="products" element={<FarmerProducts />} />
                      <Route path="*" element={<Navigate to="/farmer" replace />} />
                    </Route>
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
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
