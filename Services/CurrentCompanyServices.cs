namespace DeviceManagementOnly.Services
{
    public interface ICurrentCompanyService
    {
        Guid? GetCurrentCompanyId();
    }

    // DemoAuth login ke waqt hi token me "company_id" claim daal deta hai
    // (uske Tenant.CompanyId se). Device.API khud Company data ka owner
    // nahi hai, isliye yahan sirf token se claim padhte hain — koi DB
    // lookup nahi.
    public class CurrentCompanyService : ICurrentCompanyService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentCompanyService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? GetCurrentCompanyId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return null;

            var claim = user.FindFirst("company_id")?.Value;
            return Guid.TryParse(claim, out var companyId) ? companyId : null;
        }
    }
}