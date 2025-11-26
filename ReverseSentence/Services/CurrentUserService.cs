namespace ReverseSentence.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string GetUserId()
        {
            var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
            
            if (string.IsNullOrEmpty(username))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }

            return username;
        }
    }
}
