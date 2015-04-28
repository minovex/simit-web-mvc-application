namespace Simit.Web.Mvc.Application
{
    #region Using Directives

    using System.Web.Mvc;

    #endregion Using Directives

    /// <summary>
    /// Requested URL Type
    /// </summary>
    public enum RedirectURLType
    {
        /// <summary>
        /// The login required
        /// </summary>
        LoginRequired,

        /// <summary>
        /// The access denied
        /// </summary>
        AccessDenied,

        /// <summary>
        /// The model is null
        /// </summary>
        ModelIsNull
    }

    /// <summary>
    /// Application Context Interface
    /// </summary>
    /// <typeparam name="TSession">The type of the session.</typeparam>
    public interface IApplicationContext<TSession>
    {
        /// <summary>
        /// Gets or sets the session object.
        /// </summary>
        /// <value>
        /// The session object.
        /// </value>
        TSession SessionObject { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is user logged in.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is user logged in; otherwise, <c>false</c>.
        /// </value>
        bool IsUserLoggedIn { get; }

        /// <summary>
        /// Gets the redirect URL.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        string GetRedirectURL(RedirectURLType type);

        /// <summary>
        /// Executings the specified session user.
        /// </summary>
        /// <param name="sessionUser">The session user.</param>
        /// <param name="filterContext">The filter context.</param>
        void Executing(TSession sessionUser, ActionExecutingContext filterContext);

        /// <summary>
        /// Executed the specified filter context.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void Executed(ActionExecutingContext filterContext);

        /// <summary>
        /// Authorizes the page.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        bool AuthorizePage(string url);

        /// <summary>
        /// Gets the return URL data.
        /// </summary>
        /// <value>
        /// The return URL data.
        /// </value>
        URLData ReturnURLData { get; }
    }
}