const productModules = import.meta.glob("./product-*.jpg", { eager: true, import: "default" }) as Record<string, string>;
const farmerModules = import.meta.glob("./farmer-*.{jpg,webp}", { eager: true, import: "default" }) as Record<string, string>;
const categoryModules = import.meta.glob("./cat-*.jpg", { eager: true, import: "default" }) as Record<string, string>;
const customerModules = import.meta.glob("./customer-*.jpg", { eager: true, import: "default" }) as Record<string, string>;

function extractKey(path: string, prefix: string) {
  return path.replace(`./${prefix}-`, "").replace(/\.(jpg|webp)$/, "");
}

export const productPhotos: Record<number, string> = Object.fromEntries(
  Object.entries(productModules).map(([path, url]) => [Number(extractKey(path, "product")), url]),
);

export const farmerPhotos: Record<number, string> = Object.fromEntries(
  Object.entries(farmerModules).map(([path, url]) => [Number(extractKey(path, "farmer")), url]),
);

export const categoryPhotos: Record<string, string> = Object.fromEntries(
  Object.entries(categoryModules).map(([path, url]) => [extractKey(path, "cat"), url]),
);

export const customerPhotos: string[] = Object.entries(customerModules)
  .sort(([a], [b]) => a.localeCompare(b, undefined, { numeric: true }))
  .map(([, url]) => url);

export { default as heroPhoto } from "./hero-farmer.jpg";
export { default as treePhoto } from "./tree.png";
export { default as applePhoto } from "./apple.png";

export function getCustomerPhoto(seed: number) {
  return customerPhotos[seed % customerPhotos.length];
}
