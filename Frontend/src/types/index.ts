import type { LucideIcon } from "lucide-react";

export interface Category {
  id: number;
  name: string;
  nameTj?: string;
  nameEn?: string;
  slug: string;
  description: string;
  icon: LucideIcon;
  photoKey: string;
  imageUrl: string | null;
  productCount: number;
}

export interface Farmer {
  id: number;
  userId: number;
  farmName: string;
  ownerName: string;
  avatarUrl?: string;
  region: string;
  district: string;
  village: string;
  bio: string;
  story: string;
  rating: number;
  reviewCount: number;
  productCount: number;
  yearsActive: number;
  verified: boolean;
  responseRate?: number;
  tags: string[];
  joinedAt: string;
}

export type ProductBadge = "organic" | "new" | "bestseller" | "discount" | "premium";

export interface Product {
  id: number;
  title: string;
  titleTj?: string;
  titleEn?: string;
  slug: string;
  categoryId: number;
  farmerId: number;
  description: string;
  descriptionTj?: string;
  descriptionEn?: string;
  shortDescription: string;
  unit: string;
  photoUrl?: string;
  retailPricePerKg: number;
  oldPrice?: number;
  wholesalePricePerKg?: number;
  wholesaleMinimumQuantity?: number;
  availableQuantity: number;
  minimumOrderQuantity: number;
  harvestDate: string;
  createdAt: string;
  qualityGrade: string;
  region: string;
  district: string;
  rating: number;
  reviewCount: number;
  viewCount: number;
  orderCount: number;
  badges: ProductBadge[];
  specifications: { label: string; value: string }[];
  status: "active" | "outOfStock";
}

export interface Review {
  id: number;
  productId: number;
  customerName: string;
  rating: number;
  comment: string;
  createdAt: string;
  verifiedPurchase: boolean;
  helpfulCount: number;
  farmerReply?: { message: string; createdAt: string };
}

export interface Testimonial {
  id: number;
  name: string;
  role: string;
  quote: string;
  rating: number;
}

export interface FaqItem {
  id: number;
  question: string;
  answer: string;
  group: string;
}

export interface CartLine {
  productId: number;
  quantity: number;
}
