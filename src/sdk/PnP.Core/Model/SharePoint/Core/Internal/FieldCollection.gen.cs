﻿using PnP.Core.QueryModel.Model;
using PnP.Core.Services;
using System.Threading.Tasks;

namespace PnP.Core.Model.SharePoint
{
    internal partial class FieldCollection : QueryableDataModelCollection<IField>, IFieldCollection
    {
        public FieldCollection(PnPContext context, IDataModelParent parent, string memberName = null)
            : base(context, parent, memberName)
        {
            this.PnPContext = context;
            this.Parent = parent;
        }
    }
}
