namespace Simit.Web.Mvc.Application
{
    #region Using Directives

    using Simit.ComponentModel.DataAnnotations;
    using Simit.Web.Mvc.Application.Filters;
    using Simit.Web.MVC.Application.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Web.Mvc;

    #endregion Using Directives

    /// <summary>
    /// Controller Base
    /// </summary>
    /// <typeparam name="ApplicationContext">The type of the application context.</typeparam>
    /// <typeparam name="TModelFactory">The type of the model factory.</typeparam>
    /// <typeparam name="TSession">The type of the session.</typeparam>
    public class ControllerBase<ApplicationContext, TModelFactory, TSession> : Controller
        where ApplicationContext : IApplicationContext<TSession>, new()
        where TSession : class, new()
        where TModelFactory : class
    {
        #region Private Fields

        /// <summary>
        /// The context
        /// </summary>
        private IApplicationContext<TSession> context;

        /// <summary>
        /// The modal factory
        /// </summary>
        private TModelFactory modalFactory;

        /// <summary>
        /// The messsage container name
        /// </summary>
        private const string MesssageContainerName = "messageContainer";

        /// <summary>
        /// The add message for next page
        /// </summary>
        private bool addMessageForNextPage = true;

        #endregion Private Fields

        #region Protected Properties

        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        protected IApplicationContext<TSession> Context
        {
            get
            {
                if (this.context == null)
                {
                    this.context = Activator.CreateInstance<ApplicationContext>();
                }

                return this.context;
            }
        }

        /// <summary>
        /// Gets the model factory.
        /// </summary>
        /// <value>
        /// The model factory.
        /// </value>
        protected TModelFactory ModelFactory
        {
            get
            {
                if (this.modalFactory == null)
                {
                    this.modalFactory = (TModelFactory)Activator.CreateInstance(typeof(TModelFactory), this.Context.SessionObject);
                }

                return this.modalFactory;
            }
        }

        /// <summary>
        /// Gets the current HTTP method.
        /// </summary>
        /// <value>
        /// The current HTTP method.
        /// </value>
        /// <exception cref="System.Exception"></exception>
        protected HttpVerbs CurrentHttpMethod
        {
            get
            {
                CultureInfo culture = new CultureInfo("en-US");
                string httpMethod = culture.TextInfo.ToTitleCase(Request.HttpMethod.ToLower());
                HttpVerbs verb;

                if (Enum.TryParse(httpMethod, out verb))
                    return verb;
                else
                    throw new Exception(httpMethod + " not found in System.Web.Mvc.HttpVerbs");
            }
        }

        #endregion Protected Properties

        #region Protected Methods

        /// <summary>
        /// Called before the action method is invoked.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action.</param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!filterContext.IsChildAction)
            {
                bool hasAttribute = filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AuthenticationNotRequired), false) || filterContext.ActionDescriptor.IsDefined(typeof(AuthenticationNotRequired), false);

                string currentURL = filterContext.RequestContext.HttpContext.Request.Url.PathAndQuery;

                if (this.Context.ReturnURLData == null)
                    throw new ArgumentNullException("ReturnURLData");

                if (!hasAttribute)
                {
                    if (!this.Context.IsUserLoggedIn)
                    {
                        filterContext.Result = new RedirectResult(this.AddReturnURL(this.Context.GetRedirectURL(RedirectURLType.LoginRequired), currentURL));
                    }
                    else if (!this.Context.AuthorizePage(currentURL))
                    {
                        filterContext.Result = new RedirectResult(this.AddReturnURL(this.Context.GetRedirectURL(RedirectURLType.AccessDenied), currentURL));
                    }
                }
            }

            if (this.Context.ReturnURLData != null && !string.IsNullOrEmpty(filterContext.HttpContext.Request.QueryString[this.Context.ReturnURLData.QueryStringName]))
            {
                filterContext.ActionParameters[this.Context.ReturnURLData.ActionParameterName] = filterContext.HttpContext.Request.QueryString[this.Context.ReturnURLData.QueryStringName];
            }

            this.Context.Executing(this.Context.SessionObject, filterContext);

            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Called after the action method is invoked.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action.</param>
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (!filterContext.IsChildAction && IsValidResult(filterContext))
            {
                if (this.IsIModel(filterContext))
                {
                    IModel model = this.GetIModel(filterContext);

                    AlertMessageAttribute.HttpMethod method = (AlertMessageAttribute.HttpMethod)Enum.Parse(typeof(AlertMessageAttribute.HttpMethod), filterContext.RequestContext.HttpContext.Request.HttpMethod.ToUpper());

                    ExecuteModelAttiributes(method, model);

                    if (model.Redirect != null)
                    {
                        filterContext.Result = new RedirectResult(model.Redirect.URL);
                    }
                }
            }

            base.OnActionExecuted(filterContext);
        }

        /// <summary>
        /// Views the with check model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        protected ActionResult ViewWithCheckModel(object model)
        {
            if (model == null)
                return base.Redirect(this.Context.GetRedirectURL(RedirectURLType.ModelIsNull));

            return base.View(model);
        }

        /// <summary>
        /// Executes the model attiributes.
        /// </summary>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="model">The model.</param>
        /// <param name="overrideForNextPage">if set to <c>true</c> [override for next page].</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        protected void ExecuteModelAttiributes(AlertMessageAttribute.HttpMethod httpMethod, IModel model, bool overrideForNextPage = false)
        {
            AddMessageForNextPage(model);

            if (!ModelState.IsValid)
            {
                foreach (string error in this.GetModelStateErrors())
                {
                    model.AddMessage(Alert.Type.Error, error);
                }
            }

            if (model.IsValid)
            {
                Type modelType = model.GetType();

                CultureInfo cultureInfo = new CultureInfo("en-US");

                IEnumerable<AlertMessageAttribute> alertMessages = modelType.GetCustomAttributes(typeof(AlertMessageAttribute), false).Cast<AlertMessageAttribute>().Where(c => c.ForHttpMethod.HasFlag(httpMethod));

                if (alertMessages.Count() > 0)
                {
                    foreach (AlertMessageAttribute attribute in alertMessages)
                    {
                        PropertyInfo property = modelType.GetProperty(attribute.PropertyName);
                        if (property == null)
                        {
                            throw new ArgumentException(attribute.PropertyName + " not found");
                        }

                        Type propertyType = property.PropertyType;
                        if ((property.PropertyType == attribute.ExpectedType) || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == attribute.ExpectedType))
                        {
                            bool isValidForAttiribute = attribute.IsValid(property.GetValue(model, null));
                            if (isValidForAttiribute)
                            {
                                string message = string.IsNullOrEmpty(attribute.Message) ? this.GetReourceProperty(attribute.MessageResourceType, attribute.MessageResourceName) : attribute.Message;

                                string formattedMessage = message;

                                if (!overrideForNextPage && attribute.ForNextPage)
                                {
                                    this.AddMessageForNextPage(attribute.Type, message);
                                    if (this.addMessageForNextPage)
                                        this.addMessageForNextPage = false;
                                }
                                else
                                {
                                    model.AddMessage(attribute.Type, message);
                                }

                                if (attribute.Type == Alert.Type.Error)
                                {
                                    ModelState.AddModelError(property.Name, message);
                                }
                            }
                        }
                        else
                        {
                            throw new ArgumentException(attribute.PropertyName + " is not " + attribute.ExpectedType.FullName);
                        }
                    }
                }
            }
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Adds the return URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns></returns>
        private string AddReturnURL(string url, string returnUrl)
        {
            string redirectUrl = url;
            string queryChar = redirectUrl.IndexOf("?") == -1 ? "?" : "&";

            return redirectUrl + queryChar + this.Context.ReturnURLData.QueryStringName + "=" + Url.Encode(returnUrl);
        }

        /// <summary>
        /// Gets the model state errors.
        /// </summary>
        /// <returns></returns>
        private List<string> GetModelStateErrors()
        {
            List<string> errors = new List<string>();

            foreach (ModelState modelState in ViewData.ModelState.Values)
            {
                errors.AddRange(modelState.Errors.Select(c => c.ErrorMessage).ToArray());
            }

            return errors;
        }

        /// <summary>
        /// Gets the reource property.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        private string GetReourceProperty(Type type, string resourceName)
        {
            PropertyInfo property = type.GetProperty(resourceName);
            if (property == null)
            {
                throw new ArgumentException(resourceName + " not found");
            }
            return property.GetValue(null, null).ToString();
        }

        /// <summary>
        /// Gets the i model.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private IModel GetIModel(ActionExecutedContext filterContext)
        {
            if (filterContext.Result is ViewResultBase)
                return (filterContext.Result as ViewResultBase).Model as IModel;
            else if (filterContext.Result is JsonResult)
                return (filterContext.Result as JsonResult).Data as IModel;

            throw new NotImplementedException(filterContext.Result.ToString());
        }

        /// <summary>
        /// Determines whether [is i model] [the specified filter context].
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private bool IsIModel(ActionExecutedContext filterContext)
        {
            if (filterContext.Result is ViewResultBase)
                return (filterContext.Result as ViewResultBase).Model is IModel;
            else if (filterContext.Result is JsonResult)
                return (filterContext.Result as JsonResult).Data is IModel;

            throw new NotImplementedException(filterContext.Result.ToString());
        }

        /// <summary>
        /// Determines whether [is valid result] [the specified filter context].
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        /// <returns></returns>
        private bool IsValidResult(ActionExecutedContext filterContext)
        {
            return (filterContext.Result is ViewResultBase || filterContext.Result is JsonResult);
        }

        /// <summary>
        /// Gets the custom attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="modelType">Type of the model.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <returns></returns>
        private IEnumerable<T> GetCustomAttributes<T>(Type modelType, string httpMethod) where T : AlertMessageAttribute
        {
            AlertMessageAttribute.HttpMethod method = (AlertMessageAttribute.HttpMethod)Enum.Parse(typeof(AlertMessageAttribute.HttpMethod), CultureInfo.CurrentCulture.TextInfo.ToTitleCase(httpMethod.ToLower()));

            return modelType.GetCustomAttributes(typeof(T), false).Cast<T>().Where(c => c.ForHttpMethod.HasFlag(method));
        }

        /// <summary>
        /// Adds the message for next page.
        /// </summary>
        /// <param name="model">The model.</param>
        private void AddMessageForNextPage(IModel model)
        {
            if (addMessageForNextPage)
            {
                List<KeyValuePair<Alert.Type, string>> list = Session[MesssageContainerName] as List<KeyValuePair<Alert.Type, string>>;
                if (list != null)
                {
                    foreach (KeyValuePair<Alert.Type, string> item in list)
                    {
                        model.AddMessage(item.Key, item.Value);
                    }

                    Session[MesssageContainerName] = null;
                }
            }
        }

        /// <summary>
        /// Adds the message for next page.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="message">The message.</param>
        private void AddMessageForNextPage(Alert.Type type, string message)
        {
            if (Session[MesssageContainerName] == null)
            {
                Session[MesssageContainerName] = new List<KeyValuePair<Alert.Type, string>>();
            }
            List<KeyValuePair<Alert.Type, string>> list = Session[MesssageContainerName] as List<KeyValuePair<Alert.Type, string>>;

            list.Add(new KeyValuePair<Alert.Type, string>(type, message));

            Session[MesssageContainerName] = list;
        }

        #endregion Private Methods
    }
}