export interface CartItem {
  productNo: string;
  productName: string;
  quantity: number;
  price: number;
  imageFile?: string;
}

export interface Cart {
  username: string;
  items: CartItem[];
  totalPrice?: number;
}

export interface BasketCheckout {
  username: string;
  firstName: string;
  lastName: string;
  emailAddress: string;
  shippingAddress: string;
  invoiceAddress: string;
  totalPrice: number;
}
