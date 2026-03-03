using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BarideWeb.Data;
using BarideWeb.Models;

namespace BarideWeb.Services
{
    public interface ITenantService
    {
        Guid? GetCurrentTenantId();
    }

    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private Guid? _tenantId;
        private bool _resolved;

        public TenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? GetCurrentTenantId()
        {
            if (_resolved) return _tenantId;
            _resolved = true;

            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId");
            if (claim != null && Guid.TryParse(claim.Value, out var tid))
            {
                _tenantId = tid;
            }

            return _tenantId;
        }
    }
}
