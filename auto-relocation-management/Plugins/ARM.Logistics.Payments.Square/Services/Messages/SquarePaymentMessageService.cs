using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Core.Events;
using Nop.Services.Localization;
using Nop.Services.Messages;

namespace ARM.Logistics.Payments.Square.Services.Messages
{
    /// <summary>
    /// Square payment message service
    /// </summary>
    public partial class SquarePaymentMessageService : ISquarePaymentMessageService
    {
        #region Fields

        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ILocalizationService _localizationService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly ITokenizer _tokenizer;
        private readonly IStoreContext _storeContext;
        private readonly IEventPublisher _eventPublisher;

        #endregion

        #region Ctor

        public SquarePaymentMessageService(EmailAccountSettings emailAccountSettings,
            IEmailAccountService emailAccountService,
            ILocalizationService localizationService,
            IMessageTokenProvider messageTokenProvider,
            IMessageTemplateService messageTemplateService,
            IQueuedEmailService queuedEmailService,
            ITokenizer tokenizer,
            IStoreContext storeContext,
            IEventPublisher eventPublisher)
        {
            _emailAccountSettings = emailAccountSettings;
            _emailAccountService = emailAccountService;
            _localizationService = localizationService;
            _messageTokenProvider = messageTokenProvider;
            _messageTemplateService = messageTemplateService;
            _queuedEmailService = queuedEmailService;
            _tokenizer = tokenizer;
            _storeContext = storeContext;
            _eventPublisher = eventPublisher;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get active message templates by the name
        /// </summary>
        /// <param name="messageTemplateName">Message template name</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of message templates
        /// </returns>
        protected virtual async Task<IList<MessageTemplate>> GetActiveMessageTemplatesAsync(string messageTemplateName)
        {
            //get message templates by the name

            //Order.AmazonChecklist
            var messageTemplates = await _messageTemplateService.GetMessageTemplatesByNameAsync(messageTemplateName);
            //no template found
            if (!messageTemplates?.Any() ?? true)
                return new List<MessageTemplate>();

            //filter active templates
            messageTemplates = messageTemplates.Where(messageTemplate => messageTemplate.IsActive).ToList();

            return messageTemplates;
        }

        /// <summary>
        /// Get EmailAccount to use with a message templates
        /// </summary>
        /// <param name="messageTemplate">Message template</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the emailAccount
        /// </returns>
        protected virtual async Task<EmailAccount> GetEmailAccountOfMessageTemplateAsync(MessageTemplate messageTemplate)
        {
            var emailAccountId = await _localizationService.GetLocalizedAsync(messageTemplate, mt => mt.EmailAccountId);
            //some 0 validation (for localizable "Email account" dropdownlist which saves 0 if "Standard" value is chosen)
            if (emailAccountId == 0)
                emailAccountId = messageTemplate.EmailAccountId;

            var emailAccount = (await _emailAccountService.GetEmailAccountByIdAsync(emailAccountId) ?? await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId)) ??
                               (await _emailAccountService.GetAllEmailAccountsAsync()).FirstOrDefault();
            return emailAccount;
        }

        /// <summary>
        /// Get email and name to send email for store owner
        /// </summary>
        /// <param name="messageTemplateEmailAccount">Message template email account</param>
        /// <returns>Email address and name to send email fore store owner</returns>
        protected virtual async Task<(string email, string name)> GetStoreOwnerNameAndEmailAsync(EmailAccount messageTemplateEmailAccount)
        {
            if (messageTemplateEmailAccount == null)
                return (string.Empty, string.Empty);

            var storeOwnerEmailAccount = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);

            storeOwnerEmailAccount ??= messageTemplateEmailAccount;

            return (storeOwnerEmailAccount.Email, storeOwnerEmailAccount.DisplayName);
        }

        #endregion

        #region Methods

        #region Payment details email notification

        /// <summary>
        /// Send deposit payment details email notification
        /// </summary>
        /// <param name="dictionary">The dictionary collection</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the queued email identifier
        /// </returns>
        public virtual async Task<IList<int>> SendDepositPaymentDetailsEmailNotificationAsync(Dictionary<string, string> dictionary)
        {
            if (dictionary == null || dictionary.Count <= 0)
            {
                return new List<int>();
            }

            var messageTemplates = await GetActiveMessageTemplatesAsync(SquarePaymentDefaults.MessageTemplate.DepositPaymentDetails);

            if (!messageTemplates.Any())
            {
                return new List<int>();
            }

            if (dictionary.TryGetValue("Email", out var email) && string.IsNullOrWhiteSpace(email))
            {
                return new List<int>();
            }

            _ = dictionary.TryGetValue("Name", out var name);
            _ = dictionary.TryGetValue("PaymentUrl", out var paymentUrl);
            _ = dictionary.TryGetValue("Deposit", out var deposit);
            _ = dictionary.TryGetValue("PendingAmount", out var pendingAmount);
            _ = dictionary.TryGetValue("VehicleTypes", out var vehicleTypes);

            //tokens
            var commonTokens = new List<Token>()
            {
                { new Token("SquarePayment.PaymentURL", paymentUrl, true) },
                { new Token("SquarePayment.Deposit", deposit) },
                { new Token("SquarePayment.PendingAmount", pendingAmount) },
                { new Token("SquarePayment.VehicleTypes", vehicleTypes) }
            };

            return await messageTemplates.SelectAwait(async messageTemplate =>
            {
                //email account
                var emailAccount = await GetEmailAccountOfMessageTemplateAsync(messageTemplate);

                var tokens = new List<Token>(commonTokens);
                var store = await _storeContext.GetCurrentStoreAsync();
                await _messageTokenProvider.AddStoreTokensAsync(tokens, store, emailAccount);

                //event notification
                await _eventPublisher.MessageTokensAddedAsync(messageTemplate, tokens);

                return await SendNotificationAsync(messageTemplate, emailAccount, tokens, email, name);
            }).ToListAsync();
        }

        /// <summary>
        /// Send full payment details email notification
        /// </summary>
        /// <param name="dictionary">The dictionary collection</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the queued email identifier
        /// </returns>
        public virtual async Task<IList<int>> SendFullPaymentDetailsEmailNotificationAsync(Dictionary<string, string> dictionary)
        {
            if (dictionary == null || dictionary.Count <= 0)
            {
                return new List<int>();
            }

            var messageTemplates = await GetActiveMessageTemplatesAsync(SquarePaymentDefaults.MessageTemplate.FullPaymentDetails);

            if (!messageTemplates.Any())
            {
                return new List<int>();
            }

            if (dictionary.TryGetValue("Email", out var email) && string.IsNullOrWhiteSpace(email))
            {
                return new List<int>();
            }

            _ = dictionary.TryGetValue("Name", out var name);
            _ = dictionary.TryGetValue("PaymentUrl", out var paymentUrl);
            _ = dictionary.TryGetValue("FullPayment", out var fullPayment);
            _ = dictionary.TryGetValue("VehicleTypes", out var vehicleTypes);

            //tokens
            var commonTokens = new List<Token>()
            {
                { new Token("SquarePayment.PaymentURL", paymentUrl, true) },
                { new Token("SquarePayment.FullPayment", fullPayment) },
                { new Token("SquarePayment.VehicleTypes", vehicleTypes) }
            };

            return await messageTemplates.SelectAwait(async messageTemplate =>
            {
                //email account
                var emailAccount = await GetEmailAccountOfMessageTemplateAsync(messageTemplate);

                var tokens = new List<Token>(commonTokens);
                var store = await _storeContext.GetCurrentStoreAsync();
                await _messageTokenProvider.AddStoreTokensAsync(tokens, store, emailAccount);

                //event notification
                await _eventPublisher.MessageTokensAddedAsync(messageTemplate, tokens);

                return await SendNotificationAsync(messageTemplate, emailAccount, tokens, email, name);
            }).ToListAsync();
        }

        /// <summary>
        /// Send custom payment details email notification
        /// </summary>
        /// <param name="dictionary">The dictionary collection</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the queued email identifier
        /// </returns>
        public virtual async Task<IList<int>> SendCustomPaymentDetailsEmailNotificationAsync(Dictionary<string, string> dictionary)
        {
            if (dictionary == null || dictionary.Count <= 0)
            {
                return new List<int>();
            }

            var messageTemplates = await GetActiveMessageTemplatesAsync(SquarePaymentDefaults.MessageTemplate.AdditionalPaymentDetails);

            if (!messageTemplates.Any())
            {
                return new List<int>();
            }

            if (dictionary.TryGetValue("Email", out var email) && string.IsNullOrWhiteSpace(email))
            {
                return new List<int>();
            }

            _ = dictionary.TryGetValue("Name", out var name);
            _ = dictionary.TryGetValue("PaymentUrl", out var paymentUrl);
            _ = dictionary.TryGetValue("AdditionalAmount", out var additionalAmount);
            _ = dictionary.TryGetValue("VehicleTypes", out var vehicleTypes);

            //tokens
            var commonTokens = new List<Token>()
            {
                { new Token("SquarePayment.PaymentURL", paymentUrl, true) },
                { new Token("SquarePayment.AdditionalAmount", additionalAmount) },
                { new Token("SquarePayment.VehicleTypes", vehicleTypes) }
            };

            return await messageTemplates.SelectAwait(async messageTemplate =>
            {
                //email account
                var emailAccount = await GetEmailAccountOfMessageTemplateAsync(messageTemplate);

                var tokens = new List<Token>(commonTokens);
                var store = await _storeContext.GetCurrentStoreAsync();
                await _messageTokenProvider.AddStoreTokensAsync(tokens, store, emailAccount);

                //event notification
                await _eventPublisher.MessageTokensAddedAsync(messageTemplate, tokens);

                return await SendNotificationAsync(messageTemplate, emailAccount, tokens, email, name);
            }).ToListAsync();
        }

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
        public virtual async Task<int> SendNotificationAsync(MessageTemplate messageTemplate,
            EmailAccount emailAccount, IList<Token> tokens, string toEmailAddress, string toName,
            string attachmentFilePath = null, string attachmentFileName = null,
            string replyToEmailAddress = null, string replyToName = null,
            string fromEmail = null, string fromName = null, string subject = null,
            string ccEmail = null)
        {
            if (messageTemplate == null)
                throw new ArgumentNullException(nameof(messageTemplate));

            if (emailAccount == null)
                throw new ArgumentNullException(nameof(emailAccount));

            //retrieve localized message template data
            var bcc = await _localizationService.GetLocalizedAsync(messageTemplate, mt => mt.BccEmailAddresses);
            if (string.IsNullOrEmpty(subject))
                subject = await _localizationService.GetLocalizedAsync(messageTemplate, mt => mt.Subject);
            var body = await _localizationService.GetLocalizedAsync(messageTemplate, mt => mt.Body);

            //Replace subject and body tokens 
            var subjectReplaced = _tokenizer.Replace(subject, tokens, false);
            var bodyReplaced = _tokenizer.Replace(body, tokens, true);

            //limit name length
            toName = CommonHelper.EnsureMaximumLength(toName, 300);

            var email = new QueuedEmail
            {
                Priority = QueuedEmailPriority.High,
                From = !string.IsNullOrEmpty(fromEmail) ? fromEmail : emailAccount.Email,
                FromName = !string.IsNullOrEmpty(fromName) ? fromName : emailAccount.DisplayName,
                To = toEmailAddress,
                ToName = toName,
                ReplyTo = replyToEmailAddress,
                ReplyToName = replyToName,
                CC = !string.IsNullOrEmpty(ccEmail) ? ccEmail : string.Empty,
                Bcc = bcc,
                Subject = subjectReplaced,
                Body = bodyReplaced,
                AttachmentFilePath = attachmentFilePath,
                AttachmentFileName = attachmentFileName,
                AttachedDownloadId = messageTemplate.AttachedDownloadId,
                CreatedOnUtc = DateTime.UtcNow,
                EmailAccountId = emailAccount.Id,
                DontSendBeforeDateUtc = !messageTemplate.DelayBeforeSend.HasValue ? null
                    : (DateTime?)(DateTime.UtcNow + TimeSpan.FromHours(messageTemplate.DelayPeriod.ToHours(messageTemplate.DelayBeforeSend.Value)))
            };

            await _queuedEmailService.InsertQueuedEmailAsync(email);
            return email.Id;
        }

        #endregion

        #endregion
    }
}