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
import { CourierLayout } from "@/components/layout/CourierLayout";
import { ProtectedRoute } from "@/components/layout/ProtectedRoute";
import { PageLoader } from "@/components/layout/PageLoader";
import { Home } from "@/pages/Home";

const Catalog = lazy(() => import("@/pages/Catalog").then((m) => ({ default: m.Catalog })));
const ProductDetails = lazy(() => import("@/pages/ProductDetails").then((m) => ({ default: m.ProductDetails })));
const FarmerPublicProfile = lazy(() => import("@/pages/FarmerPublicProfile").then((m) => ({ default: m.FarmerPublicProfile })));
const Checkout = lazy(() => import("@/pages/Checkout").then((m) => ({ default: m.Checkout })));
const About = lazy(() => import("@/pages/About").then((m) => ({ default: m.About })));
const Contact = lazy(() => import("@/pages/Contact").then((m) => ({ default: m.Contact })));
const Terms = lazy(() => import("@/pages/Terms").then((m) => ({ default: m.Terms })));
const Privacy = lazy(() => import("@/pages/Privacy").then((m) => ({ default: m.Privacy })));
const Login = lazy(() => import("@/pages/Login").then((m) => ({ default: m.Login })));
const Register = lazy(() => import("@/pages/Register").then((m) => ({ default: m.Register })));
const ForgotPassword = lazy(() => import("@/pages/ForgotPassword").then((m) => ({ default: m.ForgotPassword })));
const Forbidden = lazy(() => import("@/pages/Forbidden").then((m) => ({ default: m.Forbidden })));
const NotFound = lazy(() => import("@/pages/NotFound").then((m) => ({ default: m.NotFound })));
const AdminDashboard = lazy(() => import("@/pages/AdminDashboard").then((m) => ({ default: m.AdminDashboard })));
const AdminOrders = lazy(() => import("@/pages/AdminOrders").then((m) => ({ default: m.AdminOrders })));
const AdminFarmers = lazy(() => import("@/pages/AdminFarmers").then((m) => ({ default: m.AdminFarmers })));
const AdminFarmerDetail = lazy(() => import("@/pages/AdminFarmerDetail").then((m) => ({ default: m.AdminFarmerDetail })));
const AdminCatalog = lazy(() => import("@/pages/AdminCatalog").then((m) => ({ default: m.AdminCatalog })));
const AdminDocuments = lazy(() => import("@/pages/AdminDocuments").then((m) => ({ default: m.AdminDocuments })));
const AdminCouriers = lazy(() => import("@/pages/AdminCouriers").then((m) => ({ default: m.AdminCouriers })));
const AdminDeliveryZones = lazy(() => import("@/pages/AdminDeliveryZones").then((m) => ({ default: m.AdminDeliveryZones })));
const AdminCommissions = lazy(() => import("@/pages/AdminCommissions").then((m) => ({ default: m.AdminCommissions })));
const AdminUsers = lazy(() => import("@/pages/AdminUsers").then((m) => ({ default: m.AdminUsers })));
const AdminCustomerDetail = lazy(() => import("@/pages/AdminCustomerDetail").then((m) => ({ default: m.AdminCustomerDetail })));
const AdminReviews = lazy(() => import("@/pages/AdminReviews").then((m) => ({ default: m.AdminReviews })));
const AdminSettings = lazy(() => import("@/pages/AdminSettings").then((m) => ({ default: m.AdminSettings })));
const AdminProfile = lazy(() => import("@/pages/AdminProfile").then((m) => ({ default: m.AdminProfile })));
const AdminSupport = lazy(() => import("@/pages/AdminSupport").then((m) => ({ default: m.AdminSupport })));
const AdminAiConversationLogs = lazy(() => import("@/pages/AdminAiConversationLogs").then((m) => ({ default: m.AdminAiConversationLogs })));
const AdminBlockedAccounts = lazy(() => import("@/pages/AdminBlockedAccounts").then((m) => ({ default: m.AdminBlockedAccounts })));
const AdminNotifications = lazy(() => import("@/pages/AdminNotifications").then((m) => ({ default: m.AdminNotifications })));
const FarmerDashboard = lazy(() => import("@/pages/FarmerDashboard").then((m) => ({ default: m.FarmerDashboard })));
const FarmerProducts = lazy(() => import("@/pages/FarmerProducts").then((m) => ({ default: m.FarmerProducts })));
const FarmerProfile = lazy(() => import("@/pages/FarmerProfile").then((m) => ({ default: m.FarmerProfile })));
const FarmerOrders = lazy(() => import("@/pages/FarmerOrders").then((m) => ({ default: m.FarmerOrders })));
const FarmerMessages = lazy(() => import("@/pages/FarmerMessages").then((m) => ({ default: m.FarmerMessages })));
const FarmerReviews = lazy(() => import("@/pages/FarmerReviews").then((m) => ({ default: m.FarmerReviews })));
const FarmerDocuments = lazy(() => import("@/pages/FarmerDocuments").then((m) => ({ default: m.FarmerDocuments })));
const FarmerNotifications = lazy(() => import("@/pages/FarmerNotifications").then((m) => ({ default: m.FarmerNotifications })));
const CustomerDashboard = lazy(() => import("@/pages/CustomerDashboard").then((m) => ({ default: m.CustomerDashboard })));
const CustomerOrders = lazy(() => import("@/pages/CustomerOrders").then((m) => ({ default: m.CustomerOrders })));
const CustomerMessages = lazy(() => import("@/pages/CustomerMessages").then((m) => ({ default: m.CustomerMessages })));
const CustomerProfile = lazy(() => import("@/pages/CustomerProfile").then((m) => ({ default: m.CustomerProfile })));
const CustomerNotifications = lazy(() => import("@/pages/CustomerNotifications").then((m) => ({ default: m.CustomerNotifications })));
const Wallet = lazy(() => import("@/pages/Wallet").then((m) => ({ default: m.Wallet })));
const CourierDeliveries = lazy(() => import("@/pages/CourierDeliveries").then((m) => ({ default: m.CourierDeliveries })));
const CourierProfile = lazy(() => import("@/pages/CourierProfile").then((m) => ({ default: m.CourierProfile })));
const CourierDocuments = lazy(() => import("@/pages/CourierDocuments").then((m) => ({ default: m.CourierDocuments })));
const CourierNotifications = lazy(() => import("@/pages/CourierNotifications").then((m) => ({ default: m.CourierNotifications })));

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
                      <Route path="orders" element={<AdminOrders />} />
                      <Route path="catalog" element={<AdminCatalog />} />
                      <Route path="farmers" element={<AdminFarmers />} />
                      <Route path="farmers/:id" element={<AdminFarmerDetail />} />
                      <Route path="documents" element={<AdminDocuments />} />
                      <Route path="farmer-documents" element={<Navigate to="/admin/documents?type=farmer" replace />} />
                      <Route path="courier-documents" element={<Navigate to="/admin/documents?type=courier" replace />} />
                      <Route path="couriers" element={<AdminCouriers />} />
                      <Route path="delivery-zones" element={<AdminDeliveryZones />} />
                      <Route path="users" element={<AdminUsers />} />
                      <Route path="users/:id" element={<AdminCustomerDetail />} />
                      <Route path="reviews" element={<AdminReviews />} />
                      <Route path="commissions" element={<AdminCommissions />} />
                      <Route path="support" element={<AdminSupport />} />
                      <Route path="ai-conversation-logs" element={<AdminAiConversationLogs />} />
                      <Route path="blocked-accounts" element={<AdminBlockedAccounts />} />
                      <Route path="notifications" element={<AdminNotifications />} />
                      <Route path="settings" element={<AdminSettings />} />
                      <Route path="profile" element={<AdminProfile />} />
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
                      <Route path="wallet" element={<Wallet />} />
                      <Route path="notifications" element={<FarmerNotifications />} />
                      <Route path="*" element={<Navigate to="/farmer" replace />} />
                    </Route>
                  </Route>

                  <Route element={<ProtectedRoute role="Customer" />}>
                    <Route path="customer" element={<CustomerLayout />}>
                      <Route index element={<CustomerDashboard />} />
                      <Route path="orders" element={<CustomerOrders />} />
                      <Route path="messages" element={<CustomerMessages />} />
                      <Route path="wallet" element={<Wallet />} />
                      <Route path="notifications" element={<CustomerNotifications />} />
                      <Route path="profile" element={<CustomerProfile />} />
                      <Route path="*" element={<Navigate to="/customer" replace />} />
                    </Route>
                  </Route>

                  <Route element={<ProtectedRoute role="Courier" />}>
                    <Route path="courier" element={<CourierLayout />}>
                      <Route index element={<CourierDeliveries />} />
                      <Route path="profile" element={<CourierProfile />} />
                      <Route path="documents" element={<CourierDocuments />} />
                      <Route path="notifications" element={<CourierNotifications />} />
                      <Route path="*" element={<Navigate to="/courier" replace />} />
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
                    <Route path="terms" element={<Terms />} />
                    <Route path="privacy" element={<Privacy />} />
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
