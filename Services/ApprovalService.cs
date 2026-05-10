using System.Text.Json;
using StokBarangMAUI.Models;

namespace StokBarangMAUI.Services
{
    public class ApprovalService
    {
        private readonly string _approvalFile;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        public ApprovalService()
        {
            _approvalFile = Path.Combine(FileSystem.AppDataDirectory, "pending_approvals.json");
        }

        // Submit draft untuk approval
        public async Task<string> SubmitForApprovalAsync(string projectId, string createdBy, 
            string type, object draftData, string summary)
        {
            var approval = new PendingApproval
            {
                ProjectId = projectId,
                CreatedBy = createdBy,
                Type = type,
                DraftDataJson = JsonSerializer.Serialize(draftData, _jsonOpts),
                Summary = summary,
                Status = "Pending"
            };

            var approvals = await LoadAllAsync();
            approvals.Add(approval);
            await SaveAllAsync(approvals);
            
            return approval.Id;
        }

        // Get semua pending approvals
        public async Task<List<PendingApproval>> GetPendingApprovalsAsync(string? projectId = null)
        {
            var all = await LoadAllAsync();
            var pending = all.Where(a => a.Status == "Pending").ToList();
            
            if (!string.IsNullOrWhiteSpace(projectId))
                pending = pending.Where(a => a.ProjectId == projectId).ToList();
            
            return pending.OrderByDescending(a => a.CreatedAt).ToList();
        }

        // Get count pending approvals untuk badge
        public async Task<int> GetPendingCountAsync(string? projectId = null)
        {
            var pending = await GetPendingApprovalsAsync(projectId);
            return pending.Count;
        }

        // Approve draft
        public async Task<bool> ApproveAsync(string approvalId, string approvedBy)
        {
            var approvals = await LoadAllAsync();
            var approval = approvals.FirstOrDefault(a => a.Id == approvalId);
            if (approval == null) return false;

            approval.Status = "Approved";
            approval.ApprovedBy = approvedBy;
            approval.ApprovedAt = DateTime.Now;
            
            await SaveAllAsync(approvals);
            return true;
        }

        // Reject draft
        public async Task<bool> RejectAsync(string approvalId, string rejectedBy, string reason)
        {
            var approvals = await LoadAllAsync();
            var approval = approvals.FirstOrDefault(a => a.Id == approvalId);
            if (approval == null) return false;

            approval.Status = "Rejected";
            approval.ApprovedBy = rejectedBy;
            approval.ApprovedAt = DateTime.Now;
            approval.RejectionReason = reason;
            
            await SaveAllAsync(approvals);
            return true;
        }

        // Get approval by ID
        public async Task<PendingApproval?> GetByIdAsync(string approvalId)
        {
            var approvals = await LoadAllAsync();
            return approvals.FirstOrDefault(a => a.Id == approvalId);
        }

        // Delete approval setelah diproses
        public async Task DeleteAsync(string approvalId)
        {
            var approvals = await LoadAllAsync();
            approvals.RemoveAll(a => a.Id == approvalId);
            await SaveAllAsync(approvals);
        }

        // Load all approvals from file
        private async Task<List<PendingApproval>> LoadAllAsync()
        {
            try
            {
                if (!File.Exists(_approvalFile))
                    return new List<PendingApproval>();

                var json = await File.ReadAllTextAsync(_approvalFile);
                return JsonSerializer.Deserialize<List<PendingApproval>>(json, _jsonOpts) 
                    ?? new List<PendingApproval>();
            }
            catch
            {
                return new List<PendingApproval>();
            }
        }

        // Save all approvals to file
        private async Task SaveAllAsync(List<PendingApproval> approvals)
        {
            try
            {
                var json = JsonSerializer.Serialize(approvals, _jsonOpts);
                await File.WriteAllTextAsync(_approvalFile, json);
            }
            catch { }
        }
    }
}
