namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string RedirectUrl { get; set; }

        public static ApiResponse Ok(string message = "عملیات با موفقیت انجام شد", object data = null, string redirectUrl = null)
        {
            return new ApiResponse { Success = true, Message = message, Data = data, RedirectUrl = redirectUrl };
        }

        public static ApiResponse Fail(string message = "خطایی رخ داده است", string redirectUrl = null)
        {
            return new ApiResponse { Success = false, Message = message, RedirectUrl = redirectUrl };
        }
    }
}
