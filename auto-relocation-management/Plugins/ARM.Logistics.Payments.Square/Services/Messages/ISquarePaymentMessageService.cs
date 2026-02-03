using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Messages;
using Nop.Services.Messages;

namespace ARM.Logistics.Payments.Square.Services.Messages
{
    /// <summary>
    /// Square payment message service interface
    /// </summary>
    public partial interface ISquarePaymentMessageService
    {
        #region Payment details email notification

        /// <summary>
        /// Send deposit payment details email notification
        /// </summary>
        /// <param name="dictionary">The dictionary collection</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the queued email identifier
        /// </returns>
        Task<IList<int>> SendDepositPaymentDetailsEmailNotificationAsync(Dictionary<string, string> dictionary);

        /// <summary>
        /// Send full payment details email notification
        /// </summary>
        /// <param name="dictionary">The dictionary collection</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the queued email identifier
        /// </returns>
        Task<IList<int>> SendFullPaymentDetailsEmailNotificationAsync(Dictionary<string, string> dictionary);

        /// <summary>
        /// Send custom payment details email notification
        /// </summary>
        /// <param name="dictionary">The dictionary collection</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the queued email identifier
        /// </returns>
        Task<IList<int>> SendCustomPaymentDetailsEmailNotificationAsync(Dictionary<string, string> dictionary);

        #endregion

        #region Common

        /// <summary>
        /// Send notification
        /// </summary>
        /// <param name="messageTemplate">Message template</param>
        /// <param name="emailAccount">Email account</param>
        /// <param name="tokens">Tokens</param>
        /// <param name="toEmailAddress">Recipient email address</param>
        /// <param name="toName">Recipient name</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name</param>
        /// <param name="replyToEmailAddress">"Reply to" email</param>
        /// <param name="replyToName">"Reply to" name</param>
        /// <param name="fromEmail">Sender email. If specified, then it overrides passed "emailAccount" details</param>
        /// <param name="fromName">Sender name. If specified, then it overrides passed "emailAccount" details</param>
        /// <param name="subject">Subject. If specified, then it overrides subject of a message template</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the queued email identifier
        /// </returns>
        Task<int> SendNotificationAsync(MessageTemplate messageTemplate,
            EmailAccount emailAccount, IList<Token> tokens, string toEmailAddress, string toName,
            string attachmentFilePath = null, string attachmentFileName = null,
            string replyToEmailAddress = null, string replyToName = null,
            string fromEmail = null, string fromName = null, string subject = null,
            string ccEmail = null);

        #endregion
    }
}