using System;


namespace Infocus.WebApi.Common
{
    public class AddSalesOrderResponse : BaseWebApiResponse
    {
        private DocumentKey _documentKey = new DocumentKey();
        public DocumentKey DocumentKey
        {
            get
            {
                return _documentKey;
            }
            set
            {
                _documentKey = value;
            }
        }
            
    }
}
