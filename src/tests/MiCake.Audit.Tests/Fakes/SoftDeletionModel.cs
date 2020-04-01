﻿using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Audit.Tests.Fakes
{
    public class SoftDeletionModel : Entity, ISoftDeletion
    {
        public bool IsDeleted { get; set; }
    }
}
