using System.Linq;
using System.Threading.Tasks;
using FireflyWebImporter.BusinessLayer.Nordigen;
using FireflyWebImporter.Models;
using FireflyWebImporter.Models.Import;
using Microsoft.AspNetCore.Mvc;

namespace FireflyWebImporter.Controllers
{
    public partial class ImportController : Controller
    {
        #region Fields

        private readonly INordigenManager _nordigenManager;

        #endregion

        #region Constructors

        public ImportController(INordigenManager nordigenManager)
        {
            _nordigenManager = nordigenManager;
        }

        #endregion

        #region Methods

        public virtual async Task<ActionResult> Index()
        {
            var requisitions = await _nordigenManager.GetRequisitions();
            var transactions = await _nordigenManager.GetAccountTransactions(requisitions.First().Accounts.FirstOrDefault());
            
            var model = new ImportIndexPageModel
            {
                ConfigurationStartUrl = Url.Action(MVC.Configuration.ActionNames.Index, MVC.Configuration.Name),
                RequisitionList = new RequisitionList(requisitions)
            };
            return View(model);
        }

        #endregion
    }
}