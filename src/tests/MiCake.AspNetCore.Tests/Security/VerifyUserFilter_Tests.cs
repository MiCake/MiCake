using MiCake.AspNetCore.Identity;
using MiCake.AspNetCore.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.AspNetCore.Tests.Security
{
    public class VerifyUserFilter_Tests
    {
        const string controllerMehtodName = "GetM";
        private readonly ActionExecutionDelegate FilterDelegate = () => { return Task.FromResult<ActionExecutedContext>(null); };

        [Fact]
        public async Task VerifyCurrentUser_DefaultController_HasNoAnyAttribute()
        {
            // Arrange
            var context = CreateHttpContext(null);
            var actionExecContext = CreateActionExecutingContext(new Dictionary<string, object>(), controllerMehtodName, typeof(CommonController), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task VerifyCurrentUser_HasAttributeAction_ButNoUserClaim()
        {
            // Arrange
            var context = CreateHttpContext(null);
            var actionValue = new Dictionary<string, object>()
            {
                {"id",123}
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_PrimitivesType_HasAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(302, response.StatusCode);
        }

        [Fact]
        public async Task VerifyCurrentUser_HasAttributeAction_HasRightClaim()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "123") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",123}
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_PrimitivesType_HasAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task VerifyCurrentUser_HasAttributeAction_HasWrongClaim()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "339") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",123}
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_PrimitivesType_HasAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(302, response.StatusCode);   //because value is different.
        }

        [Fact]
        public async Task VerifyCurrentUser_HasMoreAttribute()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "123") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",123}
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_PrimitivesType_HasMoreAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
              {
                  await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);
              });   //beacuse has more CurrentUserAttribute.
        }

        [Fact]
        public async Task VerifyCurrentUser_HasNoAttributeAction_HasSameClaim()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "123") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",123}
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_PrimitivesType_HasNoAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(200, response.StatusCode);   //because has no attribute.
        }

        [Fact]
        public async Task VerifyCurrentUser_HasNoAttributeAction_HasDifferentClaim()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "123") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",339}
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_PrimitivesType_HasNoAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(200, response.StatusCode);   //because has no attribute.
        }

        [Fact]
        public async Task VerifyCurrentUser_HasModelAttribute_WithRightClaim()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "123") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",new UserDtoWithAttribute(){ Id ="123"} }
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_ModelType_HasAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task VerifyCurrentUser_HasModelAttribute_WithWrongClaim()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "524") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",new UserDtoWithAttribute(){ Id ="123"} }
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_ModelType_HasAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(302, response.StatusCode);
        }

        [Fact]
        public async Task VerifyCurrentUser_HasModelAttribute_ModelNoVerifyID_WithWrongClaim()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "524") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",new UserDtoWithAttribute(){ Id ="123"} }
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_ModelType_HasAttribute_NoVerifyIdMarked), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(302, response.StatusCode);
        }

        [Fact]
        public async Task VerifyCurrentUser_HasModelAttribute_ModelNoVerifyID_WithSameClaim()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "123") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",new UserDtoWithAttribute(){ Id ="123"} }
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_ModelType_HasAttribute_NoVerifyIdMarked), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(302, response.StatusCode);  //because this model has no verifyUserIDAttribute.
        }

        [Fact]
        public async Task VerifyCurrentUser_HasModelAttribute_ModelHasMoreVerifyID()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "123") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",new UserDtoWithMoreAttribute(){ Id ="123",Name="123"} }
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_ModelType_HasAttribute_MoreVerifyIdMarked), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            // Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
           {
               await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);
           });
        }

        [Fact]
        public async Task VerifyCurrentUser_WithGuidType()
        {
            // Arrange
            var useIDValue = Guid.NewGuid();
            var claims = new List<Claim>() { new Claim("userid", useIDValue.ToString()) };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",useIDValue }
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_GuidType__HasAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task VerifyCurrentUser_WithStructType()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "123") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",new UserStructDto("123") }
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_CustomerStructType__HasAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task VerifyCurrentUser_ComplexType()
        {
            // Arrange
            var claims = new List<Claim>() { new Claim("userid", "123") };
            var context = CreateHttpContext(CreatePrincipal(claims));
            var actionValue = new Dictionary<string, object>()
            {
                {"id",new Version(1,2,3) }
            };
            var actionExecContext = CreateActionExecutingContext(actionValue, controllerMehtodName, typeof(CommonController_VersionType__HasAttribute), context);

            // Act
            var filter = new VerifyCurrentUserFilter(Options.Create(new MiCakeIdentityOptions()));
            await filter.OnActionExecutionAsync(actionExecContext, FilterDelegate);

            // Assert
            var response = context.Response;
            Assert.Equal(302, response.StatusCode);  //beacuse version to string is not same with 123.
        }

        #region Data required
        public ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        public HttpContext CreateHttpContext(ClaimsPrincipal claimsPrincipal)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie();

            var context = new DefaultHttpContext();
            context.User = claimsPrincipal;
            context.RequestServices = services.BuildServiceProvider();

            return context;
        }

        private ActionExecutingContext CreateActionExecutingContext(
            Dictionary<string, object> value,
            string methodName = null,
            Type controllerType = null,
            HttpContext httpContext = null)
        {
            var controllerDes = CreateActionDescriptor(methodName, controllerType);
            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), controllerDes);

            return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), value, null);
        }

        private ControllerActionDescriptor CreateActionDescriptor(string methodName = null, Type controllerType = null)
        {
            var action = new ControllerActionDescriptor();
            action.SetProperty(new ApiDescriptionActionData());

            if (controllerType != null)
            {
                action.MethodInfo = controllerType.GetMethod(
                    methodName ?? "ReturnsObject",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                action.ControllerTypeInfo = controllerType.GetTypeInfo();
                action.BoundProperties = new List<ParameterDescriptor>();

                foreach (var property in controllerType.GetProperties())
                {
                    var bindingInfo = BindingInfo.GetBindingInfo(property.GetCustomAttributes().OfType<object>());
                    if (bindingInfo != null)
                    {
                        action.BoundProperties.Add(new ParameterDescriptor()
                        {
                            BindingInfo = bindingInfo,
                            Name = property.Name,
                            ParameterType = property.PropertyType,
                        });
                    }
                }
            }
            else
            {
                action.MethodInfo = GetType().GetMethod(
                    methodName ?? "ReturnsObject",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            }

            action.Parameters = new List<ParameterDescriptor>();
            foreach (var parameter in action.MethodInfo.GetParameters())
            {
                action.Parameters.Add(new ControllerParameterDescriptor()
                {
                    Name = parameter.Name,
                    ParameterType = parameter.ParameterType,
                    BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes().OfType<object>()),
                    ParameterInfo = parameter
                });
            }

            return action;
        }
    }
    #endregion

    public class CommonController
    {
        public void GetM()
        {
        }
    }

    public class CommonController_PrimitivesType_HasNoAttribute
    {
        public void GetM(int id)
        {
        }
    }

    public class CommonController_PrimitivesType_HasAttribute
    {
        public void GetM([CurrentUser] int id)
        {
        }
    }

    public class CommonController_PrimitivesType_HasMoreAttribute
    {
        public void GetM([CurrentUser] int id, [CurrentUser] Guid dd)
        {
        }
    }

    public class CommonController_ModelType_HasAttribute
    {
        public void GetM([CurrentUser] UserDtoWithAttribute id)
        {
        }
    }

    public class CommonController_ModelType_HasAttribute_NoVerifyIdMarked
    {
        public void GetM([CurrentUser] UserDto id)
        {
        }
    }

    public class CommonController_GuidType__HasAttribute
    {
        public void GetM([CurrentUser] Guid id)
        {
        }
    }

    public class CommonController_CustomerStructType__HasAttribute
    {
        public void GetM([CurrentUser] UserStructDto id)
        {
        }
    }

    public class CommonController_VersionType__HasAttribute
    {
        public void GetM([CurrentUser] Version id)
        {
        }
    }

    public class CommonController_ModelType_HasAttribute_MoreVerifyIdMarked
    {
        public void GetM([CurrentUser] UserDtoWithMoreAttribute id)
        {
        }
    }

    #region Model
    public class UserDtoWithAttribute
    {
        [VerifyUserId()]
        public string Id { get; set; }
    }

    public class UserDtoWithMoreAttribute
    {
        [VerifyUserId()]
        public string Id { get; set; }

        [VerifyUserId()]
        public string Name { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; }
    }

    public struct UserStructDto
    {
        [VerifyUserId]
        public string Id { get; set; }

        public UserStructDto(string id)
        {
            Id = id;
        }
    }
    #endregion
}
