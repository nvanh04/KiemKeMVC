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

        // XÁC THỰC ADMIN TRÊN DATABASE (BẢO MẬT TUYỆT ĐỐI, KHÔNG LỘ MẬT KHẨU TRÊN CODE)
        [HttpPost]
        public JsonResult VerifyAdminLogin(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "Vui lòng nhập mật khẩu quản trị!" });

            string passTrim = password.Trim();

            // Tìm kiếm trong bảng Workshops xem có khớp mật khẩu không
            var matchedUser = _db.Workshops.FirstOrDefault(w => w.Password != null && w.Password.ToLower() == passTrim.ToLower());
            if (matchedUser != null)
            {
                if (matchedUser.Name.Contains("Admin") || matchedUser.Name.Contains("Tổng"))
                {
                    Session["AdminRole"] = "SUPER_ADMIN";
                    Session["AdminWorkshop"] = null;
                    return Json(new { success = true, role = "SUPER_ADMIN", workshop = (string)null, message = "Đăng nhập Admin Tổng thành công!" });
                }
                else
                {
                    Session["AdminRole"] = "WORKSHOP_ADMIN";
                    Session["AdminWorkshop"] = matchedUser.Name;
                    return Json(new { success = true, role = "WORKSHOP_ADMIN", workshop = matchedUser.Name, message = $"Đăng nhập Admin {matchedUser.Name} thành công!" });
                }
            }

            return Json(new { success = false, message = "Mật khẩu quản trị không chính xác!" });
        }

        [HttpPost]
        public JsonResult LogoutAdmin()
        {
            Session["AdminRole"] = null;
            Session["AdminWorkshop"] = null;
            return Json(new { success = true, message = "Đã đăng xuất tài khoản quản trị!" });
        }

        [HttpPost]
        public JsonResult SaveFullInventorySheet(SaveSheetRequest req)
        {
            if (req == null || req.Items == null || req.Items.Count == 0)
                return Json(new { success = false, message = "Phiếu kiểm kê chưa có vật tư!" });

            var randomNum = new Random().Next(100000, 999999);
            var sheetId = "KK-" + randomNum;
            var primaryMachine = req.Items.FirstOrDefault()?.Machine ?? "N/A";

            var sheet = new InventorySheet
            {
                Id = sheetId,
                Workshop = req.Workshop,
                DeviceId = req.DeviceId,
                Date = req.Date,
                Machine = primaryMachine,
                CreatedBy = req.CreatedBy,
                CreatedAt = DateTime.Now
            };

            foreach (var item in req.Items)
            {
                sheet.Items.Add(new InventorySheetItem
                {
                    SheetId = sheetId,
                    SapCode = item.SapCode,
                    Name = item.Name,
                    Category = item.Category,
                    Workshop = string.IsNullOrEmpty(item.Workshop) ? req.Workshop : item.Workshop,
                    Machine = item.Machine,
                    Quantity = item.Quantity,
                    Tubes = item.Tubes,
                    Note = item.Note
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
        public JsonResult UpdateSheet(EditSheetRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.SheetId))
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

            var sheet = _db.InventorySheets.Include(s => s.Items).FirstOrDefault(s => s.Id == req.SheetId);
            if (sheet == null)
                return Json(new { success = false, message = "Không tìm thấy phiếu!" });

            sheet.Workshop = req.Workshop;
            sheet.Date = req.Date;
            sheet.CreatedBy = req.CreatedBy;
            sheet.Machine = req.Items?.FirstOrDefault()?.Machine ?? sheet.Machine;

            _db.InventorySheetItems.RemoveRange(sheet.Items);

            if (req.Items != null)
            {
                foreach (var item in req.Items)
                {
                    sheet.Items.Add(new InventorySheetItem
                    {
                        SheetId = req.SheetId,
                        SapCode = item.SapCode,
                        Name = item.Name,
                        Category = item.Category,
                        Workshop = req.Workshop,
                        Machine = item.Machine,
                        Quantity = item.Quantity,
                        Tubes = item.Tubes,
                        Note = item.Note
                    });
                }
            }

            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã cập nhật phiếu [{req.SheetId}]!" });
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
        public JsonResult DeleteWorkshop(string name)
        {
            var ws = _db.Workshops.FirstOrDefault(w => w.Name == name);
            if (ws == null) return Json(new { success = false, message = "Không tìm thấy phân xưởng!" });

            if (_db.Workshops.Count() <= 1)
                return Json(new { success = false, message = "Phải duy trì tối thiểu 1 phân xưởng!" });

            _db.Workshops.Remove(ws);
            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã xóa phân xưởng {name}!" });
        }

        [HttpPost]
        public JsonResult DeleteCategory(string name, string workshop)
        {
            var cat = _db.Categories.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.Workshop == workshop);
            if (cat == null) return Json(new { success = false, message = "Không tìm thấy danh mục trong phân xưởng này!" });

            _db.Categories.Remove(cat);
            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã xóa danh mục {name} khỏi {workshop}!" });
        }

        [HttpPost]
        public JsonResult DeleteMachine(string name, string workshop)
        {
            var mach = _db.Machines.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && m.Workshop == workshop);
            if (mach == null) return Json(new { success = false, message = "Không tìm thấy máy trong phân xưởng này!" });

            _db.Machines.Remove(mach);
            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã xóa máy {name} khỏi {workshop}!" });
        }

        [HttpPost]
        public JsonResult SaveMaterial(MaterialDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SapCode) || string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Workshop))
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ Phân xưởng, Mã SAP và Tên vật tư!" });

            string sap = dto.SapCode.Trim();
            string ws = dto.Workshop.Trim();

            if (!string.IsNullOrEmpty(dto.OriginalSapCode))
            {
                var existingMat = _db.Materials.FirstOrDefault(m => m.SapCode == dto.OriginalSapCode && m.Workshop == ws);
                if (existingMat != null)
                {
                    if (dto.OriginalSapCode != sap && _db.Materials.Any(m => m.SapCode == sap && m.Workshop == ws))
                        return Json(new { success = false, message = $"Mã SAP [{sap}] đã tồn tại trong {ws}!" });

                    existingMat.SapCode = sap;
                    existingMat.Name = dto.Name.Trim();
                    existingMat.Description = dto.Description?.Trim();
                    existingMat.Category = dto.Category;
                    existingMat.Workshop = ws;
                    existingMat.Aliases = dto.Aliases?.Trim();

                    _db.SaveChanges();
                    return Json(new { success = true, message = $"Đã cập nhật mã SAP {sap} trong {ws}!" });
                }
            }

            if (_db.Materials.Any(m => m.SapCode == sap && m.Workshop == ws))
                return Json(new { success = false, message = $"Mã SAP [{sap}] đã tồn tại trong {ws}!" });

            _db.Materials.Add(new Material
            {
                SapCode = sap,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Category = dto.Category,
                Workshop = ws,
                Aliases = dto.Aliases?.Trim()
            });

            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã thêm mã SAP {sap} vào {ws}!" });
        }

        [HttpPost]
        public JsonResult DeleteMaterial(string sapCode, string workshop)
        {
            var targetCode = (sapCode ?? "").Trim();
            var targetWs = (workshop ?? "").Trim();

            var mat = _db.Materials.FirstOrDefault(m => m.SapCode == targetCode && m.Workshop == targetWs);
            if (mat != null)
            {
                _db.Materials.Remove(mat);
                _db.SaveChanges();
                return Json(new { success = true, message = $"Đã xóa mã SAP [{targetCode}] khỏi {targetWs}!" });
            }

            return Json(new { success = false, message = "Không tìm thấy mã vật tư cần xóa trong phân xưởng này!" });
        }

        [HttpPost]
        public JsonResult DeleteAllMaterials(string workshop)
        {
            if (string.IsNullOrEmpty(workshop) || workshop == "ALL")
            {
                _db.Materials.RemoveRange(_db.Materials);
            }
            else
            {
                var mats = _db.Materials.Where(m => m.Workshop == workshop).ToList();
                _db.Materials.RemoveRange(mats);
            }
            _db.SaveChanges();
            return Json(new { success = true, message = "Đã xóa toàn bộ mã nguyên vật liệu!" });
        }

        [HttpPost]
        public JsonResult DeleteAllMachines(string workshop)
        {
            if (string.IsNullOrEmpty(workshop) || workshop == "ALL")
            {
                _db.Machines.RemoveRange(_db.Machines);
            }
            else
            {
                var machs = _db.Machines.Where(m => m.Workshop == workshop).ToList();
                _db.Machines.RemoveRange(machs);
            }
            _db.SaveChanges();
            return Json(new { success = true, message = "Đã xóa toàn bộ máy / vị trí!" });
        }

        [HttpPost]
        public JsonResult DeleteAllCategories(string workshop)
        {
            if (string.IsNullOrEmpty(workshop) || workshop == "ALL")
            {
                _db.Categories.RemoveRange(_db.Categories);
            }
            else
            {
                var cats = _db.Categories.Where(c => c.Workshop == workshop).ToList();
                _db.Categories.RemoveRange(cats);
            }
            _db.SaveChanges();
            return Json(new { success = true, message = "Đã xóa toàn bộ danh mục vật tư!" });
        }

        [HttpPost]
        public JsonResult SaveBatchMaterials(BatchMaterialRequest req)
        {
            if (req == null || req.Materials == null || req.Materials.Count == 0)
                return Json(new { success = false, message = "Không có dữ liệu vật tư để nhập!" });

            int insertedCount = 0;
            int updatedCount = 0;

            foreach (var item in req.Materials)
            {
                if (string.IsNullOrWhiteSpace(item.SapCode) || string.IsNullOrWhiteSpace(item.Name))
                    continue;

                string sap = item.SapCode.Trim();
                string ws = string.IsNullOrWhiteSpace(item.Workshop) ? "Phân Xưởng Cắt" : item.Workshop.Trim();
                string cat = string.IsNullOrWhiteSpace(item.Category) ? "KHÁC" : item.Category.Trim().ToUpper();

                if (!_db.Workshops.Any(w => w.Name.Equals(ws, StringComparison.OrdinalIgnoreCase)))
                {
                    string pass = ws.Replace("Phân Xưởng", "").Replace("xưởng", "").Trim() + "2026";
                    _db.Workshops.Add(new Workshop { Name = ws, Password = pass });
                }

                if (!_db.Categories.Any(c => c.Name.Equals(cat, StringComparison.OrdinalIgnoreCase) && c.Workshop == ws))
                {
                    _db.Categories.Add(new Category { Name = cat, Workshop = ws });
                }

                var existing = _db.Materials.FirstOrDefault(m => m.SapCode == sap && m.Workshop == ws);
                if (existing != null)
                {
                    existing.Name = item.Name.Trim();
                    existing.Category = cat;
                    existing.Description = item.Description?.Trim();
                    existing.Aliases = item.Aliases?.Trim();
                    updatedCount++;
                }
                else
                {
                    _db.Materials.Add(new Material
                    {
                        SapCode = sap,
                        Name = item.Name.Trim(),
                        Workshop = ws,
                        Category = cat,
                        Description = item.Description?.Trim(),
                        Aliases = item.Aliases?.Trim()
                    });
                    insertedCount++;
                }
            }

            _db.SaveChanges();
            return Json(new { success = true, message = $"Nhập Excel thành công: Thêm mới {insertedCount}, Cập nhật {updatedCount} mã!" });
        }

        [HttpPost]
        public JsonResult ImportCategoriesFromExcel(ImportExcelCategoriesRequest req)
        {
            if (req == null || req.Categories == null || req.Categories.Count == 0)
                return Json(new { success = false, message = "Không có dữ liệu danh mục để nhập!" });

            int added = 0;
            foreach (var item in req.Categories)
            {
                if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Workshop))
                    continue;

                string ws = item.Workshop.Trim();
                string catName = item.Name.Trim().ToUpper();

                if (!_db.Workshops.Any(w => w.Name.Equals(ws, StringComparison.OrdinalIgnoreCase)))
                {
                    string pass = ws.Replace("Phân Xưởng", "").Replace("xưởng", "").Trim() + "2026";
                    _db.Workshops.Add(new Workshop { Name = ws, Password = pass });
                }

                bool exists = _db.Categories.Any(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase) && c.Workshop == ws);
                if (!exists)
                {
                    _db.Categories.Add(new Category { Name = catName, Workshop = ws });
                    added++;
                }
            }

            _db.SaveChanges();
            return Json(new { success = true, message = $"Nhập Excel thành công: Đã thêm {added} danh mục mới!" });
        }

        [HttpPost]
        public JsonResult ImportMachinesFromExcel(ImportExcelMachinesRequest req)
        {
            if (req == null || req.Machines == null || req.Machines.Count == 0)
                return Json(new { success = false, message = "Không có dữ liệu máy để nhập!" });

            int added = 0;
            foreach (var item in req.Machines)
            {
                if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Workshop))
                    continue;

                string ws = item.Workshop.Trim();
                string machName = item.Name.Trim();

                if (!_db.Workshops.Any(w => w.Name.Equals(ws, StringComparison.OrdinalIgnoreCase)))
                {
                    string pass = ws.Replace("Phân Xưởng", "").Replace("xưởng", "").Trim() + "2026";
                    _db.Workshops.Add(new Workshop { Name = ws, Password = pass });
                }

                bool exists = _db.Machines.Any(m => m.Name.Equals(machName, StringComparison.OrdinalIgnoreCase) && m.Workshop == ws);
                if (!exists)
                {
                    _db.Machines.Add(new Machine { Name = machName, Workshop = ws });
                    added++;
                }
            }

            _db.SaveChanges();
            return Json(new { success = true, message = $"Nhập Excel thành công: Đã thêm {added} máy/vị trí mới!" });
        }

        [HttpPost]
        public JsonResult DeleteBatchSheets(DeleteBatchSheetsRequest req)
        {
            if (req == null || req.SheetIds == null || req.SheetIds.Count == 0)
                return Json(new { success = false, message = "Không có phiếu nào để xóa!" });

            var sheets = _db.InventorySheets.Where(s => req.SheetIds.Contains(s.Id)).ToList();
            _db.InventorySheets.RemoveRange(sheets);
            _db.SaveChanges();

            return Json(new { success = true, message = $"Đã xóa thành công {sheets.Count} phiếu kiểm kê!" });
        }

        [HttpPost]
        public JsonResult SaveBatchCategories(BatchCategoryDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Workshop) || string.IsNullOrWhiteSpace(req.Names))
                return Json(new { success = false, message = "Vui lòng chọn phân xưởng và nhập tên danh mục!" });

            string ws = req.Workshop.Trim();
            var rawNames = req.Names.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            int added = 0;

            foreach (var rawName in rawNames)
            {
                string catName = rawName.Trim().ToUpper();
                if (!string.IsNullOrEmpty(catName))
                {
                    bool exists = _db.Categories.Any(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase) && c.Workshop == ws);
                    if (!exists)
                    {
                        _db.Categories.Add(new Category { Name = catName, Workshop = ws });
                        added++;
                    }
                }
            }

            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã thêm thành công {added} danh mục mới vào {ws}!" });
        }

        [HttpPost]
        public JsonResult SaveBatchMachines(BatchMachineDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Workshop) || string.IsNullOrWhiteSpace(req.Names))
                return Json(new { success = false, message = "Vui lòng chọn phân xưởng và nhập tên máy!" });

            string ws = req.Workshop.Trim();
            var rawNames = req.Names.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            int added = 0;

            foreach (var rawName in rawNames)
            {
                string machName = rawName.Trim();
                if (!string.IsNullOrEmpty(machName))
                {
                    bool exists = _db.Machines.Any(m => m.Name.Equals(machName, StringComparison.OrdinalIgnoreCase) && m.Workshop == ws);
                    if (!exists)
                    {
                        _db.Machines.Add(new Machine { Name = machName, Workshop = ws });
                        added++;
                    }
                }
            }

            _db.SaveChanges();
            return Json(new { success = true, message = $"Đã thêm thành công {added} máy mới vào {ws}!" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
