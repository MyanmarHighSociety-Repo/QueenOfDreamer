using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using QueenOfDreamer.API.Const;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace QueenOfDreamer.API.Helpers
{
    public class ActionActivity : IAsyncActionFilter
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {

            var tokenValidationParamters = new TokenValidationParameters
                {
                    ValidateLifetime = QueenOfDreamerConst.TOKEN_VALIDATELIFETIME,
                    ValidateIssuerSigningKey = QueenOfDreamerConst.TOKEN_VALIDATEISSUERSIGNINGKEY,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                            .GetBytes(QueenOfDreamerConst.TOKEN_SECRET)),
                    ValidateIssuer = QueenOfDreamerConst.TOKEN_VALIDATEISSUER, 
                    ValidateAudience = QueenOfDreamerConst.TOKEN_VALIDATEAUDIENCE,
                    ValidIssuer = QueenOfDreamerConst.TOKEN_ISSUER
                };

            StringValues token;
            context.HttpContext.Request.Headers.TryGetValue("Authorization", out token);

            if(token.Count > 0)
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken securityToken;

                var principal = tokenHandler.ValidateToken(token[0].Remove(0,7), tokenValidationParamters, out securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;
                
                foreach (Claim jwtClaim in jwtSecurityToken.Claims)
                {          
                   if(jwtClaim.Type=="groupsid" && jwtClaim.Value!=QueenOfDreamerConst.APPLICATION_CONFIG_ID.ToString())
                   {
                        context.HttpContext.Response.StatusCode = 401;
                        return;
                   }
                }

                if (jwtSecurityToken == null || 
                    !jwtSecurityToken.Header.Alg.
                    Equals(QueenOfDreamerConst.TOKEN_ALG, StringComparison.InvariantCultureIgnoreCase) ||
                    DateTime.UtcNow > jwtSecurityToken.ValidTo)
                {
                    context.HttpContext.Response.StatusCode = 401;
                    return;
                }
                    
                await next();
                return;
            }

            context.Result = new JsonResult(new {HttpStatusCode.Unauthorized});
            
        }
    }
}