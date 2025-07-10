namespace SaleManagement.Entities.Enums;

public enum UserRole
{
    None = 0, //khong co vai tro chi xem dc chu khong mua dc khong ban dc
    Customer =1,
    Seller =2,
    //vi khi dung phep toan bit thi 001+010 =011=3 nen admin =3 se sai
    Admin =4,
}