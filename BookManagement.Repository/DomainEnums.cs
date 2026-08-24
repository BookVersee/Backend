namespace BookStore.BE2.Domain.Enums;

public enum UserRole { ADMIN, CUSTOMER, SHOP, DELIVER }
public enum UserStatus { ACTIVE, LOCKED }
public enum ShopCondition { OPEN, CLOSED }
public enum BookStatus { ACTIVE, EMPTY, HIDDEN }
public enum OrderStatus { PENDING, PAID, PROCESSING, SHIPPING, DELIVERING, DELIVERED, CANCELLED, FAILED, APPROVED }
public enum ReturnStatus { NONE, REQUESTED, PENDING, REJECTED, PROCESSING, SHIPPED, DELIVERED, CANCELLED, COMPLETED, REFUNDED }
public enum DeliveryStatus { PENDING, TRANSIT, DELIVERED, RETURNED }
public enum PaymentType { PAYMENT, REFUND }
public enum PaymentMethod { COD, ONLINE, BANK_TRANSFER }
public enum PaymentStatus { PENDING, SUCCESS, FAILED }
public enum ReturnReasonType { WRONG_ITEM, DAMAGED, DEFECTIVE }
public enum ReturnRequestStatus { PENDING, APPROVED, REJECTED }
public enum TransactionReferenceType { ORDER_PAYMENT, REFUND, SHIPPING_FEE, SHOP_REVENUE, WITHDRAWAL }
public enum TransactionType { IN, OUT }
public enum FeedbackType { SHOP, BOOK }
public enum NotificationType { ORDER_UPDATE, PROMOTION, SYSTEM }
