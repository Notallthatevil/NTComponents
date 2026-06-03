namespace NTComponents;

/// <summary>
///     HTML <c>autocomplete</c> attribute token constants.
/// </summary>
/// <remarks>
///     These values identify the kind of data expected by a form control so browsers and assistive technologies can offer
///     appropriate autofill behavior. Some tokens are complete values, while grouping, contact, and <see cref="WebAuthn" />
///     tokens are composed with field tokens in a space-separated <c>autocomplete</c> value.
/// </remarks>
/// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete">MDN autocomplete attribute reference</seealso>
public static class AutoComplete {

    /// <summary>
    ///     Allows browser autofill without specifying what kind of value the field expects.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#value">MDN autocomplete values</seealso>
    public const string On = "on";

    /// <summary>
    ///     Disables browser autofill for sensitive, one-time, or application-managed values.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#value">MDN autocomplete values</seealso>
    public const string Off = "off";

    /// <summary>
    ///     Prefix used to create a named autocomplete section, such as <c>section-shipping</c>, when a form repeats the same kinds of fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#named_groups">MDN named groups</seealso>
    public const string SectionPrefix = "section-";

    /// <summary>
    ///     Marks a field as part of the shipping address or shipping contact information.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#grouping_identifier">MDN grouping identifier</seealso>
    public const string Shipping = "shipping";

    /// <summary>
    ///     Marks a field as part of the billing address or billing contact information.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#grouping_identifier">MDN grouping identifier</seealso>
    public const string Billing = "billing";

    /// <summary>
    ///     Identifies a person's full name and is used for a single free-form name field.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Name = "name";

    /// <summary>
    ///     Identifies a person's title or name prefix and is used for values such as Mr., Ms., or Dr.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string HonorificPrefix = "honorific-prefix";

    /// <summary>
    ///     Identifies a person's given name and is used for first-name fields where that split is appropriate.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string GivenName = "given-name";

    /// <summary>
    ///     Identifies a person's additional names and is used for middle-name or other secondary-name fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string AdditionalName = "additional-name";

    /// <summary>
    ///     Identifies a person's family name and is used for surname or last-name fields where that split is appropriate.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string FamilyName = "family-name";

    /// <summary>
    ///     Identifies a person's name suffix and is used for values such as Jr., III, or professional suffixes.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string HonorificSuffix = "honorific-suffix";

    /// <summary>
    ///     Identifies a nickname, screen name, or handle and is used for a short informal name.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Nickname = "nickname";

    /// <summary>
    ///     Identifies an account username and is used for sign-in, registration, and account-management fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Username = "username";

    /// <summary>
    ///     Identifies a new password and is used when creating or changing an account password.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string NewPassword = "new-password";

    /// <summary>
    ///     Identifies the current account password and is used for sign-in or password-confirmation fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CurrentPassword = "current-password";

    /// <summary>
    ///     Identifies a one-time verification code and is used for MFA, login, or identity verification flows.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string OneTimeCode = "one-time-code";

    /// <summary>
    ///     Identifies a job title and is used for a person's role within an organization.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string OrganizationTitle = "organization-title";

    /// <summary>
    ///     Identifies an organization or company name and is used with person, address, or contact fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Organization = "organization";

    /// <summary>
    ///     Identifies a full street address and is used when the address may contain multiple lines.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string StreetAddress = "street-address";

    /// <summary>
    ///     Identifies the first street address line and is used when the street address is split into line fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string AddressLine1 = "address-line1";

    /// <summary>
    ///     Identifies the second street address line and is used for apartment, suite, building, or similar address details.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string AddressLine2 = "address-line2";

    /// <summary>
    ///     Identifies the third street address line and is used for additional address details when needed.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string AddressLine3 = "address-line3";

    /// <summary>
    ///     Identifies the finest administrative address level and is used for locales with four administrative levels.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string AddressLevel4 = "address-level4";

    /// <summary>
    ///     Identifies the third administrative address level and is used for locales with three or more administrative levels.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string AddressLevel3 = "address-level3";

    /// <summary>
    ///     Identifies the second administrative address level and is commonly used for city, town, village, or locality.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string AddressLevel2 = "address-level2";

    /// <summary>
    ///     Identifies the broadest administrative address level and is commonly used for state, province, canton, or region.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string AddressLevel1 = "address-level1";

    /// <summary>
    ///     Identifies an ISO 3166-1 alpha-2 country code and is used for machine-readable country selection.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Country = "country";

    /// <summary>
    ///     Identifies a country name and is used for human-readable country entry or selection.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CountryName = "country-name";

    /// <summary>
    ///     Identifies a postal code and is used for ZIP code, post code, CEDEX code, or similar mail-routing fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string PostalCode = "postal-code";

    /// <summary>
    ///     Identifies the full name on a payment instrument and is used for cardholder-name fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardName = "cc-name";

    /// <summary>
    ///     Identifies the given name on a payment instrument and is used when cardholder names are split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardGivenName = "cc-given-name";

    /// <summary>
    ///     Identifies additional names on a payment instrument and is used when cardholder names are split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardAdditionalName = "cc-additional-name";

    /// <summary>
    ///     Identifies the family name on a payment instrument and is used when cardholder names are split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardFamilyName = "cc-family-name";

    /// <summary>
    ///     Identifies a payment instrument number and is used for credit, debit, or similar card-number fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardNumber = "cc-number";

    /// <summary>
    ///     Identifies a payment instrument expiration date and is used when month and year are entered together.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardExpiration = "cc-exp";

    /// <summary>
    ///     Identifies a payment instrument expiration month and is used when month and year are entered separately.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardExpirationMonth = "cc-exp-month";

    /// <summary>
    ///     Identifies a payment instrument expiration year and is used when month and year are entered separately.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardExpirationYear = "cc-exp-year";

    /// <summary>
    ///     Identifies a payment instrument security code and is used for CSC, CVC, CVV, SPC, CCID, or similar fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardSecurityCode = "cc-csc";

    /// <summary>
    ///     Identifies the type of payment instrument and is used for values such as Visa or Mastercard.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string CreditCardType = "cc-type";

    /// <summary>
    ///     Identifies the preferred transaction currency and is used for ISO 4217 currency-code fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string TransactionCurrency = "transaction-currency";

    /// <summary>
    ///     Identifies the desired transaction amount and is used for bid, sale price, or similar amount fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string TransactionAmount = "transaction-amount";

    /// <summary>
    ///     Identifies a preferred language and is used for BCP 47 language-tag fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Language = "language";

    /// <summary>
    ///     Identifies a full birthday and is used when date of birth is entered as one date value.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Birthday = "bday";

    /// <summary>
    ///     Identifies the day component of a birthday and is used when date of birth is split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string BirthdayDay = "bday-day";

    /// <summary>
    ///     Identifies the month component of a birthday and is used when date of birth is split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string BirthdayMonth = "bday-month";

    /// <summary>
    ///     Identifies the year component of a birthday and is used when date of birth is split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string BirthdayYear = "bday-year";

    /// <summary>
    ///     Identifies gender identity and is used for free-form gender fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Sex = "sex";

    /// <summary>
    ///     Identifies a home page or related web page and is used for URL fields tied to a person, organization, address, or contact.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Url = "url";

    /// <summary>
    ///     Identifies a photograph, icon, or related image URL and is used for image fields tied to a person, organization, address, or contact.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#other_tokens">MDN other tokens</seealso>
    public const string Photo = "photo";

    /// <summary>
    ///     Marks contact information as residential and is used before telephone, email, or instant-messaging tokens.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#recipient_type">MDN recipient type tokens</seealso>
    public const string Home = "home";

    /// <summary>
    ///     Marks contact information as workplace-related and is used before telephone, email, or instant-messaging tokens.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#recipient_type">MDN recipient type tokens</seealso>
    public const string Work = "work";

    /// <summary>
    ///     Marks contact information as mobile and is used before telephone, email, or instant-messaging tokens.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#recipient_type">MDN recipient type tokens</seealso>
    public const string Mobile = "mobile";

    /// <summary>
    ///     Marks contact information as fax-related and is used before telephone, email, or instant-messaging tokens.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#recipient_type">MDN recipient type tokens</seealso>
    public const string Fax = "fax";

    /// <summary>
    ///     Marks contact information as pager-related and is used before telephone, email, or instant-messaging tokens.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#recipient_type">MDN recipient type tokens</seealso>
    public const string Pager = "pager";

    /// <summary>
    ///     Identifies a full telephone number and is used when country code, area code, and local number are entered together.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string Telephone = "tel";

    /// <summary>
    ///     Identifies a telephone country code and is used when a phone number is split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string TelephoneCountryCode = "tel-country-code";

    /// <summary>
    ///     Identifies a national telephone number without the country code and is used when a phone number is split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string TelephoneNational = "tel-national";

    /// <summary>
    ///     Identifies a telephone area code and is used when a phone number is split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string TelephoneAreaCode = "tel-area-code";

    /// <summary>
    ///     Identifies a local telephone number without country or area code and is used when a phone number is split into fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string TelephoneLocal = "tel-local";

    /// <summary>
    ///     Identifies the first part of a split local telephone number and is used when a local number is divided into prefix and suffix fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string TelephoneLocalPrefix = "tel-local-prefix";

    /// <summary>
    ///     Identifies the second part of a split local telephone number and is used when a local number is divided into prefix and suffix fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string TelephoneLocalSuffix = "tel-local-suffix";

    /// <summary>
    ///     Identifies a telephone extension and is used for internal extension-code fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string TelephoneExtension = "tel-extension";

    /// <summary>
    ///     Identifies an email address and is used for email contact fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string Email = "email";

    /// <summary>
    ///     Identifies an instant-messaging protocol endpoint and is used for IMPP URL fields.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#digital_contact_tokens">MDN digital contact tokens</seealso>
    public const string InstantMessagingProtocol = "impp";

    /// <summary>
    ///     Requests conditional WebAuthn credential suggestions and is used as the final token on supported input or textarea controls.
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/autocomplete#web_authorization_token">MDN web authorization token</seealso>
    public const string WebAuthn = "webauthn";
}
