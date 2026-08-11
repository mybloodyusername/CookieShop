namespace CookieShop.App.Exceptions;

public class DuplicateException(string message) : ConflictException(message);