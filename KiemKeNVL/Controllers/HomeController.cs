using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using KiemKeNVL.Models;

namespace KiemKeNVL.Controllers
{
    public class HomeController : Controller
    {
        private readonly KiemKeDbContext _db = new KiemKeDbContext();

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetInitialData()
        {
            var workshops = _db.Workshops.OrderBy(w => w.Id).Select(w => new { w.Id, w.Name, w.Password }).ToList();
            var categories = _db.Categories.OrderBy(c => c.Name).Select(c => new { c.Id, c.Name, c.Workshop }).ToList();
            var machines = _db.Machines.OrderBy(m => m.Name).Select(m => new { m.Id, m.Name, m.Workshop }).ToList();
            var materials = _db.Materials.OrderBy(m => m.Name).ToList().Select(m => new {
                m.Id,
                m.SapCode,
                m.Name,
                m.Description,
                m.Category,
                m.Workshop,
                Aliases = string.IsNullOrEmpty(m.Aliases) ? new string[0] : m.Aliases.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray()
            }).ToList();

            return Json(new { success = true, workshops, categories, machines, materials }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveFullInventorySheet(SaveSheetRequest req)
        {
            if (req == null || req.items == null || req.items.Count == 0)
                return Json(new { success = false, message = "Phiếu kiểm kê chưa có vật tư!" });

            var randomNum = new Random().Next(100000, 999999);
            var sheetId = "KK-" + randomNum;
            var primaryMachine = req.items.FirstOrDefault()?.machine ?? "N/A";

            var sheet = new InventorySheet
            {
                Id = sheetId,
                Workshop = req.workshop,
                DeviceId = req.deviceId,
                Date = req.date,
                Machine = primaryMachine,
                CreatedBy = req.createdBy,
                CreatedAt = DateTime.Now
            };

            foreach (var item in req.items)
            {
                sheet.Items.Add(new InventorySheetItem
                {
                    SheetId = sheetId,
                    SapCode = item.sapCode,
                    Name = item.name,
                    Category = item.category,
                    Workshop = string.IsNullOrEmpty(item.workshop) ? req.workshop : item.workshop,
                    Machine = item.machine,
                    Quantity = item.quantity,
                    Tubes = item.tubes,
                    Note = item.note
                });
            }

            _db.InventorySheets.Add(sheet);
            _db.SaveChanges();

            return Json(new { success = true, sheetId, message = $"Đã lưu phiếu [{sheetId}] vào cơ sở dữ liệu!" });
        }

        [HttpGet]
        public JsonResult GetHistory(string fromDate, string toDate, string workshopFilter, string deviceId, bool isAdmin = false, string adminRole = null, string adminWorkshop = null)
        {
            var query = _db.InventorySheets.Include(s => s.Items).AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(s => s.DeviceId == deviceId);
            }
            else if (adminRole == "WORKSHOP_ADMIN" && !string.IsNullOrEmpty(adminWorkshop))
            {
                query = query.Where(s => s.Workshop == adminWorkshop);
            }
            else if (adminRole == "SUPER_ADMIN" && !string.IsNullOrEmpty(workshopFilter) && workshopFilter != "ALL")
            {
                query = query.Where(s => s.Workshop == workshopFilter);
            }

            if (!string.IsNullOrEmpty(fromDate))
                query = query.Where(s => string.Compare(s.Date, fromDate) >= 0);

            if (!string.IsNullOrEmpty(toDate))
                query = query.Where(s => string.Compare(s.Date, toDate) <= 0);

            var list = query.OrderByDescending(s => s.CreatedAt).ToList().Select(s => new {
                s.Id,
                s.Workshop,
                s.DeviceId,
                s.Date,
                s.Machine,
                s.CreatedBy,
                ItemsCount = s.Items.Count,
                Items = s.Items.Select(it => new {
                    it.SapCode,
                    it.Name,
                    it.Category,
                    it.Workshop,
                    it.Machine,
                    it.Quantity,
                    it.Tubes,
                    it.Note
                }).ToList()
            }).ToList();

            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteSheet(string id)
        {
            var sheet = _db.InventorySheets.Find(id);
            if (sheet == null) return Json(new { success = false, message = "Không tìm thấy phiếu" });

            _db.InventorySheets.Remove(sheet);
            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã xóa phiếu [{id}]!" });
        }

        [HttpPost]
        public JsonResult SaveWorkshop(string name)
        {
            if (_db.Workshops.Any(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return Json(new { success = false, message = "Phân xưởng đã tồn tại!" });

            string pass = name.Replace("Phân Xưởng", "").Replace("xưởng", "").Trim() + "2026";
            _db.Workshops.Add(new Workshop { Name = name, Password = pass });
            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã thêm phân xưởng {name}!" });
        }

        [HttpPost]
        public JsonResult SaveCategory(string name, string workshop)
        {
            if (_db.Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.Workshop == workshop))
                return Json(new { success = false, message = "Danh mục đã có trong phân xưởng!" });

            _db.Categories.Add(new Category { Name = name.ToUpper(), Workshop = workshop });
            _db.SaveChanges();
            return Json(new { success = true, message = "Đã lưu danh mục!" });
        }

        [HttpPost]
        public JsonResult SaveMachine(string name, string workshop)
        {
            if (_db.Machines.Any(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && m.Workshop == workshop))
                return Json(new { success = false, message = "Máy/vị trí đã có trong phân xưởng!" });

            _db.Machines.Add(new Machine { Name = name, Workshop = workshop });
            _db.SaveChanges();
            return Json(new { success = true, message = "Đã thêm máy!" });
        }

        [HttpPost]
        public JsonResult SaveMaterial(string sapCode, string name, string description, string category, string workshop, string aliases)
        {
            var mat = _db.Materials.FirstOrDefault(m => m.SapCode == sapCode);
            if (mat != null)
            {
                mat.Name = name;
                mat.Description = description;
                mat.Category = category;
                mat.Workshop = workshop;
                mat.Aliases = aliases;
            }
            else
            {
                _db.Materials.Add(new Material
                {
                    SapCode = sapCode,
                    Name = name,
                    Description = description,
                    Category = category,
                    Workshop = workshop,
                    Aliases = aliases
                });
            }
            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã lưu vật tư {sapCode}!" });
        }

        [HttpPost]
        public JsonResult DeleteMaterial(string sapCode)
        {
            var mat = _db.Materials.FirstOrDefault(m => m.SapCode == sapCode);
            if (mat != null)
            {
                _db.Materials.Remove(mat);
                _db.SaveChanges();
            }
            return Json(new { success = true, message = "Đã xóa vật tư!" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}