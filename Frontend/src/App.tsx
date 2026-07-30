import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "@/context/AuthContext";
import { CartProvider } from "@/context/CartContext";
import { FavoritesProvider } from "@/context/FavoritesContext";
import { RootLayout } from "@/components/layout/RootLayout";
import { CustomerAwareShell } from "@/components/layout/CustomerAwareShell";
import { AppChrome } from "@/components/layout/AppChrome";
import { AdminLayout } from "@/components/layout/AdminLayout";
import { FarmerLayout } from "@/components/layout/FarmerLayout";
import { CustomerLayout } from "@/components/layout/CustomerLayout";
import { ProtectedRoute } from "@/components/layout/ProtectedRoute";
import { PageLoader } from "@/components/layout/PageLoader";
import { Home } from "@/pages/Home";

const Catalog = lazy(() => import("@/pages/Catalog").then((m) => ({ default: m.Catalog })));
const ProductDetails = lazy(() => import("@/pages/ProductDetails").then((m) => ({ default: m.ProductDetails })));
const FarmerPublicProfile = lazy(() => import("@/pages/FarmerPublicProfile").then((m) => ({ default: m.FarmerPublicProfile })));
const Checkout = lazy(() => import("@/pages/Checkout").then((m) => ({ default: m.Checkout })));
const About = lazy(() => import("@/pages/About").then((m) => ({ default: m.About })));
const Contact = lazy(() => import("@/pages/Contact").then((m) => ({ default: m.Contact })));
const Login = lazy(() => import("@/pages/Login").then((m) => ({ default: m.Login })));
const Register = lazy(() => import("@/pages/Register").then((m) => ({ default: m.Register })));
const ForgotPassword = lazy(() => import("@/pages/ForgotPassword").then((m) => ({ default: m.ForgotPassword })));
const Forbidden = lazy(() => import("@/pages/Forbidden").then((m) => ({ default: m.Forbidden })));
const NotFound = lazy(() => import("@/pages/NotFound").then((m) => ({ default: m.NotFound })));
const AdminDashboard = lazy(() => import("@/pages/AdminDashboard").then((m) => ({ default: m.AdminDashboard })));
const AdminStatistics = lazy(() => import("@/pages/AdminStatistics").then((m) => ({ default: m.AdminStatistics })));
const AdminOrders = lazy(() => import("@/pages/AdminOrders").then((m) => ({ default: m.AdminOrders })));
const AdminProducts = lazy(() => import("@/pages/AdminProducts").then((m) => ({ default: m.AdminProducts })));
const AdminFarmers = lazy(() => import("@/pages/AdminFarmers").then((m) => ({ default: m.AdminFarmers })));
const AdminFarmerDocuments = lazy(() => import("@/pages/AdminFarmerDocuments").then((m) => ({ default: m.AdminFarmerDocuments })));
const AdminUsers = lazy(() => import("@/pages/AdminUsers").then((m) => ({ default: m.AdminUsers })));
const AdminReviews = lazy(() => import("@/pages/AdminReviews").then((m) => ({ default: m.AdminReviews })));
const AdminSettings = lazy(() => import("@/pages/AdminSettings").then((m) => ({ default: m.AdminSettings })));
const AdminNotifications = lazy(() => import("@/pages/AdminNotifications").then((m) => ({ default: m.AdminNotifications })));
const FarmerDashboard = lazy(() => import("@/pages/FarmerDashboard").then((m) => ({ default: m.FarmerDashboard })));
const FarmerProducts = lazy(() => import("@/pages/FarmerProducts").then((m) => ({ default: m.FarmerProducts })));
const FarmerProfile = lazy(() => import("@/pages/FarmerProfile").then((m) => ({ default: m.FarmerProfile })));
const FarmerOrders = lazy(() => import("@/pages/FarmerOrders").then((m) => ({ default: m.FarmerOrders })));
const FarmerMessages = lazy(() => import("@/pages/FarmerMessages").then((m) => ({ default: m.FarmerMessages })));
const FarmerReviews = lazy(() => import("@/pages/FarmerReviews").then((m) => ({ default: m.FarmerReviews })));
const FarmerDocuments = lazy(() => import("@/pages/FarmerDocuments").then((m) => ({ default: m.FarmerDocuments })));
const FarmerStaff = lazy(() => import("@/pages/FarmerStaff").then((m) => ({ default: m.FarmerStaff })));
const FarmerNotifications = lazy(() => import("@/pages/FarmerNotifications").then((m) => ({ default: m.FarmerNotifications })));
const CustomerDashboard = lazy(() => import("@/pages/CustomerDashboard").then((m) => ({ default: m.CustomerDashboard })));
const CustomerOrders = lazy(() => import("@/pages/CustomerOrders").then((m) => ({ default: m.CustomerOrders })));
const CustomerMessages = lazy(() => import("@/pages/CustomerMessages").then((m) => ({ default: m.CustomerMessages })));
const CustomerProfile = lazy(() => import("@/pages/CustomerProfile").then((m) => ({ default: m.CustomerProfile })));
const CustomerNotifications = lazy(() => import("@/pages/CustomerNotifications").then((m) => ({ default: m.CustomerNotifications })));

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
                  <Route path="forgot-password" element={<ForgotPassword />} />

                  <Route element={<ProtectedRoute role="Admin" />}>
                    <Route path="admin" element={<AdminLayout />}>
                      <Route index element={<AdminDashboard />} />
                      <Route path="statistics" element={<AdminStatistics />} />
                      <Route path="orders" element={<AdminOrders />} />
                      <Route path="products" element={<AdminProducts />} />
                      <Route path="farmers" element={<AdminFarmers />} />
                      <Route path="farmer-documents" element={<AdminFarmerDocuments />} />
                      <Route path="users" element={<AdminUsers />} />
                      <Route path="reviews" element={<AdminReviews />} />
                      <Route path="notifications" element={<AdminNotifications />} />
                      <Route path="settings" element={<AdminSettings />} />
                      <Route path="*" element={<Navigate to="/admin" replace />} />
                    </Route>
                  </Route>

                  <Route element={<ProtectedRoute role="Farmer" />}>
                    <Route path="farmer" element={<FarmerLayout />}>
                      <Route index element={<FarmerDashboard />} />
                      <Route path="products" element={<FarmerProducts />} />
                      <Route path="orders" element={<FarmerOrders />} />
                      <Route path="messages" element={<FarmerMessages />} />
                      <Route path="reviews" element={<FarmerReviews />} />
                      <Route path="profile" element={<FarmerProfile />} />
                      <Route path="documents" element={<FarmerDocuments />} />
                      <Route path="staff" element={<FarmerStaff />} />
                      <Route path="notifications" element={<FarmerNotifications />} />
                      <Route path="*" element={<Navigate to="/farmer" replace />} />
                    </Route>
                  </Route>

                  <Route element={<ProtectedRoute role="Customer" />}>
                    <Route path="customer" element={<CustomerLayout />}>
                      <Route index element={<CustomerDashboard />} />
                      <Route path="orders" element={<CustomerOrders />} />
                      <Route path="messages" element={<CustomerMessages />} />
                      <Route path="notifications" element={<CustomerNotifications />} />
                      <Route path="profile" element={<CustomerProfile />} />
                      <Route path="*" element={<Navigate to="/customer" replace />} />
                    </Route>
                  </Route>

                  <Route element={<CustomerAwareShell />}>
                    <Route path="catalog" element={<Catalog />} />
                    <Route path="product/:slug" element={<ProductDetails />} />
                    <Route path="farmers/:id" element={<FarmerPublicProfile />} />
                    <Route path="checkout" element={<Checkout />} />
                  </Route>

                  <Route element={<RootLayout />}>
                    <Route index element={<Home />} />
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
