﻿using System;
using System.Text.Json.Serialization;
using Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identities
{
	public class UserToken : IdentityUserToken<Guid>, IAuditableEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedById { get; set; }
    }
}
