import { Hero } from "@/components/sections/Hero";
import { PopularCategories } from "@/components/sections/PopularCategories";
import { FeaturedProducts } from "@/components/sections/FeaturedProducts";
import { BestFarmers } from "@/components/sections/BestFarmers";
import { WhyChooseUs } from "@/components/sections/WhyChooseUs";
import { PlatformStats } from "@/components/sections/PlatformStats";
import { FreshHarvest } from "@/components/sections/FreshHarvest";
import { DeliveryProcess } from "@/components/sections/DeliveryProcess";
import { Testimonials } from "@/components/sections/Testimonials";
import { FAQSection } from "@/components/sections/FAQSection";
import { CTASection } from "@/components/sections/CTASection";

export function Home() {
  return (
    <>
      <Hero />
      <PopularCategories />
      <FeaturedProducts />
      <BestFarmers />
      <WhyChooseUs />
      <PlatformStats />
      <FreshHarvest />
      <DeliveryProcess />
      <Testimonials />
      <FAQSection />
      <CTASection />
    </>
  );
}
