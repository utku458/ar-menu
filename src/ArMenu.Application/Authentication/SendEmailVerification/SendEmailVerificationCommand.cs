using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Authentication.SendEmailVerification;

/// <summary>E-mails the signed-in user a new verification link; nothing happens once the address is verified.</summary>
public sealed record SendEmailVerificationCommand(UserId UserId, string? Language) : ICommand<Result>;
