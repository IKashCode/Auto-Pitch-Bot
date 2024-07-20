using System;
using System.Text;
using System.Security.Cryptography;

namespace Payment.PaymentAaio
{
    class PaymentAaio
    {
        // Id магазина.
        private string _merchantId = "2c22a680-a4f3-465e-8a47-95b27ad2ac36";
        // Ценя услуги.
        private decimal _amount = 99;
        // Валюта оплаты.
        private string _currency = "RUB";
        // Секретный ключ №1.
        private string _secret = "dcdb41d0810f67ede0a80bc8bdda3efd";
        // Id услуги.
        private string _orderId = "BuyingName";
        // Описание услуги.
        private string _desc = "";
        // Язык перевода.
        private string _language = "ru";

        public void GetPaymentLink()
        {
            string sign = string.Join(":", _merchantId, _amount, _currency, _secret, _orderId);

            using (SHA256 sHA256 = SHA256.Create())
            {
                
            }
        }
    }
}