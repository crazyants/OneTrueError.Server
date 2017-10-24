﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Griffin.Data;

namespace codeRR.Server.Web.Infrastructure
{
    public class TransactionalAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Exception == null && filterContext.Controller.ViewData.ModelState.IsValid)
            {
                var uow = (AdoNetUnitOfWork) filterContext.HttpContext.GetService(typeof(IAdoNetUnitOfWork));
                uow.SaveChanges();
            }
                
            base.OnActionExecuted(filterContext);
        }
    }
}