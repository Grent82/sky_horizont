namespace SkyHorizont.Domain.Shared;

using System;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}