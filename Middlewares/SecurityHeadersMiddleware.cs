//namespace DeviceManagementOnly.Middlewares
//{
//    // ══════════════════════════════════════════════════════════════
//    //  SECURITY HEADERS MIDDLEWARE
//    //  Har response ke saath ye headers automatically add hote hain.
//    //  Kya karta hai har header, neeche comment me:
//    // ══════════════════════════════════════════════════════════════
//    public class SecurityHeadersMiddleware
//    {
//        private readonly RequestDelegate _next;
//        public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

//        public async Task InvokeAsync(HttpContext context)
//        {
//            var headers = context.Response.Headers;

//            // Browser ko file ka Content-Type "sniff"/guess karne se roकता है —
//            // isse attacker koi .txt file ko .js/.html banake execute nahi karwa sakta.
//            headers["X-Content-Type-Options"] = "nosniff";

//            // Site ko kisi <iframe> ke andar load hone se rokta hai —
//            // clickjacking attack (invisible iframe pe click karwana) se bachaata hai.
//            headers["X-Frame-Options"] = "DENY";

//            // Purane browsers ke liye reflected-XSS filter enable karta hai.
//            // (Modern browsers me CSP zyada important hai, ye legacy support ke liye hai)
//            headers["X-XSS-Protection"] = "1; mode=block";

//            // Browser ko force karta hai ki HAMESHA HTTPS use kare, agle 1 saal (31536000 sec) tak,
//            // http:// pe koi bhi request automatically https:// pe redirect ho jaayegi.
//            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

//            // Referrer header me kitni info bhejni hai jab user kisi link pe click kare —
//            // full URL leak na ho dusri site ko, sirf origin jaaye cross-origin requests me.
//            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

//            // Browser features (camera, mic, geolocation, etc.) ko is app ke andar disable karta hai
//            // jab tak explicitly zaroorat na ho — attack surface kam karta hai.
//            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

//            // Content-Security-Policy — sabse important header:
//            // Batata hai browser ko ki scripts/styles/images/fonts KAHAN SE load karna allowed hai.
//            // Isse XSS attack me inject kiya gaya external <script src="evil.com/x.js">
//            // browser khud hi block kar dega, kyunki evil.com whitelist me nahi hai.
//            headers["Content-Security-Policy"] =
//                "default-src 'self'; " +
//                "img-src 'self' data: https:; " +
//                "script-src 'self'; " +
//                "style-src 'self' 'unsafe-inline'; " +
//                "font-src 'self' data:; " +
//                "frame-ancestors 'none'; " +
//                "object-src 'none'";

//            // Server ki technology/version chhupana — attacker ko exact stack pata na chale.
//            headers.Remove("Server");
//            headers.Remove("X-Powered-By");

//            await _next(context);
//        }
//    }

//    public static class SecurityHeadersMiddlewareExtensions
//    {
//        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
//            => app.UseMiddleware<SecurityHeadersMiddleware>();
//    }
//}




namespace DeviceManagementOnly.Middlewares
{
    // ══════════════════════════════════════════════════════════════
    // SECURITY HEADERS MIDDLEWARE
    //
    // Adds security-related HTTP response headers.
    // Swagger is allowed to work in Development while the
    // production CSP remains strict.
    // ══════════════════════════════════════════════════════════════

    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            IWebHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // ──────────────────────────────────────────────────────
            // 1. Prevent MIME type sniffing
            // ──────────────────────────────────────────────────────
            headers["X-Content-Type-Options"] = "nosniff";


            // ──────────────────────────────────────────────────────
            // 2. Prevent clickjacking
            //
            // Prevents this API/Swagger from being loaded inside
            // an iframe on another website.
            // ──────────────────────────────────────────────────────
            headers["X-Frame-Options"] = "DENY";


            // ──────────────────────────────────────────────────────
            // 3. Referrer Policy
            //
            // Sends only the origin when navigating to another
            // origin instead of leaking the complete URL.
            // ──────────────────────────────────────────────────────
            headers["Referrer-Policy"] =
                "strict-origin-when-cross-origin";


            // ──────────────────────────────────────────────────────
            // 4. Permissions Policy
            //
            // Disable browser features that this API does not need.
            // ──────────────────────────────────────────────────────
            headers["Permissions-Policy"] =
                "camera=(), " +
                "microphone=(), " +
                "geolocation=(), " +
                "payment=()";


            // ──────────────────────────────────────────────────────
            // 5. Content Security Policy
            //
            // Swagger UI uses inline JavaScript and inline styles.
            // Therefore, Development Swagger needs 'unsafe-inline'.
            //
            // Production remains strict because Swagger is normally
            // disabled there.
            // ──────────────────────────────────────────────────────

            if (_environment.IsDevelopment())
            {
                // Swagger-compatible CSP
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline'; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self' data:; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'none'; " +
                    "object-src 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'";
            }
            else
            {
                // Strict Production CSP
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self'; " +
                    "style-src 'self'; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self' data:; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'none'; " +
                    "object-src 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'";
            }


            // ──────────────────────────────────────────────────────
            // 6. HSTS
            //
            // IMPORTANT:
            // Only enable this in Production.
            //
            // HSTS tells browsers to remember that the website
            // should only be accessed through HTTPS.
            // ──────────────────────────────────────────────────────
            if (!_environment.IsDevelopment())
            {
                headers["Strict-Transport-Security"] =
                    "max-age=31536000; includeSubDomains";
            }


            // ──────────────────────────────────────────────────────
            // 7. Hide server technology information
            // ──────────────────────────────────────────────────────
            headers.Remove("Server");
            headers.Remove("X-Powered-By");


            // Continue request pipeline
            await _next(context);
        }
    }


    // ══════════════════════════════════════════════════════════════
    // Extension method
    // ══════════════════════════════════════════════════════════════

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(
            this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}