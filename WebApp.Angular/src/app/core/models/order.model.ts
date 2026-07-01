export interface Order {
  id: number;
  userName: string;
  firstName?: string;
  lastName?: string;
  emailAddress?: string;
  shippingAddress?: string;
  invoiceAddress?: string;
  totalPrice: number;
  status: number;
  createdDate: string;
}
