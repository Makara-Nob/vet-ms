using System.Drawing;

namespace VetMS.Theme;

public static class AppTheme
{
    // ── Brand Colors ────────────────────────────────────────────────────────
    public static readonly Color BrandTeal   = Color.FromArgb(0, 169, 157);     // Main brand color (from logo)
    public static readonly Color BrandDark   = Color.FromArgb(0, 121, 112);     // Darker teal for sidebar
    public static readonly Color BrandDeep   = Color.FromArgb(0, 89, 82);      // Deeper teal for active/hover
    
    // ── Global Mapping (Used by UIHelper and Forms) ────────────────────────
    public static readonly Color Primary     = BrandTeal;
    public static readonly Color Sidebar     = BrandDark;
    public static readonly Color SidebarCard = Color.FromArgb(18, 255, 255, 255); // 7% white tint
    public static readonly Color SidebarHover = Color.FromArgb(28, 255, 255, 255); // 11% white tint
    
    public static readonly Color Accent      = Color.FromArgb(0, 120, 215);      // Keep standard blue for highlights
    public static readonly Color Success     = Color.FromArgb(40, 167, 69);      
    public static readonly Color Danger      = Color.FromArgb(220, 53, 69);      
    public static readonly Color Warning     = Color.FromArgb(255, 193, 7);      
    
    public static readonly Color Neutral     = Color.FromArgb(108, 117, 125);    
    public static readonly Color LightBg     = Color.FromArgb(245, 247, 250);    
    public static readonly Color AltRow      = Color.FromArgb(235, 248, 247);    // Very light teal for grid rows
    
    // ── Components ──────────────────────────────────────────────────────────
    public static readonly Color ToastSuccess = Color.FromArgb(30, 160, 80);
    public static readonly Color ToastError   = Color.FromArgb(200, 50, 60);
    public static readonly Color HeaderHover  = Color.FromArgb(40, 80, 120); // Hover for header buttons
    
    // ── UI Constants ────────────────────────────────────────────────────────
    public static readonly Color HeaderText   = Color.White;
    public static readonly Color SubtitleText = Color.FromArgb(180, 225, 222);
}
