namespace Simit.Web.Mvc.Application
{
    /// <summary>
    ///  The URL Data Contanier
    /// </summary>
    public sealed class URLData
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the query string.
        /// </summary>
        /// <value>
        /// The name of the query string.
        /// </value>
        public string QueryStringName { get; set; }

        /// <summary>
        /// Gets or sets the name of the action parameter.
        /// </summary>
        /// <value>
        /// The name of the action parameter.
        /// </value>
        public string ActionParameterName { get; set; }

        #endregion Public Properties
    }
}