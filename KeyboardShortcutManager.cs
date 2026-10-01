using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Result of an Escape key press evaluation by the debouncer.
    /// </summary>
    public enum EscapePressResult
    {
        IgnoredBounce,
        SinglePress,
        DoublePress
    }

    /// <summary>
    /// High-precision debouncer and double-press detector for keyboard actions.
    /// Prevents mechanical switch jitter and typematic repeats while detecting
    /// consecutive double-presses within a configurable time window.
    /// </summary>
    public class DoublePressDebouncer
    {
        private readonly Stopwatch stopwatch;
        private long lastValidPressMs;
        private readonly long minDebounceIntervalMs;
        private readonly long maxDoublePressWindowMs;

        public DoublePressDebouncer(long maxWindowMs = 500, long minIntervalMs = 60)
        {
            this.maxDoublePressWindowMs = maxWindowMs;
            this.minDebounceIntervalMs = minIntervalMs;
            this.stopwatch = Stopwatch.StartNew();
            this.lastValidPressMs = -10000;
        }

        /// <summary>
        /// Registers a key press and determines if it represents a debounced bounce,
        /// a single press, or a valid consecutive double press.
        /// </summary>
        public EscapePressResult RegisterPress()
        {
            long now = stopwatch.ElapsedMilliseconds;
            long delta = now - lastValidPressMs;

            // 1. Hardware jitter / bounce filter
            if (delta < minDebounceIntervalMs)
            {
                return EscapePressResult.IgnoredBounce;
            }

            // 2. Double-press window check (within 500ms)
            if (delta <= maxDoublePressWindowMs)
            {
                // Reset so a third quick press does not immediately trigger another double-press
                lastValidPressMs = -10000;
                return EscapePressResult.DoublePress;
            }

            // 3. First press of a potential double-press sequence
            lastValidPressMs = now;
            return EscapePressResult.SinglePress;
        }

        public void Reset()
        {
            lastValidPressMs = -10000;
        }
    }

    /// <summary>
    /// Centralized, production-grade keyboard shortcut manager.
    /// Encapsulates key dispatching, debounced double-escape minimize actions,
    /// navigation routing, and productivity hotkeys.
    /// </summary>
    public class KeyboardShortcutManager
    {
        private readonly DoublePressDebouncer escapeDebouncer;

        // Action Callbacks
        public Action DoubleEscapeTriggered { get; set; }
        public Action SingleEscapeTriggered { get; set; }
        public Action MoveUpTriggered { get; set; }
        public Action MoveDownTriggered { get; set; }
        public Action CopyPasswordTriggered { get; set; }
        public Action FocusSearchTriggered { get; set; }
        public Action SaveCredentialTriggered { get; set; }
        public Action ClearFormTriggered { get; set; }
        public Action TogglePasswordTriggered { get; set; }
        public Action GeneratePasswordTriggered { get; set; }
        public Action OpenSettingsTriggered { get; set; }
        public Action RefreshTriggered { get; set; }
        public Action DeleteTriggered { get; set; }
        public Action EnterEditTriggered { get; set; }
        public Action DownToNavigateListTriggered { get; set; }
        public Action UpToNavigateSearchTriggered { get; set; }

        public KeyboardShortcutManager()
        {
            // 600ms double-tap window with 50ms debounce protection
            escapeDebouncer = new DoublePressDebouncer(600, 50);
        }

        /// <summary>
        /// Evaluates and routes key combinations intercepted in ProcessCmdKey.
        /// </summary>
        public bool HandleCmdKey(Keys keyData, Control activeCtrl, bool isSearchFocused, bool hasSearchText, int searchSelectionLen, bool hasListItems, bool hasSelectedItems, bool isTopItemSelected)
        {
            // 1. Double Escape (Debounced 500ms) -> Minimize to System Tray
            if (keyData == Keys.Escape)
            {
                EscapePressResult result = escapeDebouncer.RegisterPress();

                if (result == EscapePressResult.DoublePress)
                {
                    if (DoubleEscapeTriggered != null)
                    {
                        DoubleEscapeTriggered();
                        return true;
                    }
                }
                else if (result == EscapePressResult.SinglePress)
                {
                    if (SingleEscapeTriggered != null)
                    {
                        SingleEscapeTriggered();
                        return true;
                    }
                }
                else
                {
                    // Ignored jitter/bounce
                    return true;
                }
            }

            // 2. Alt + Up / Alt + Down (Reordering credentials)
            if (keyData == (Keys.Alt | Keys.Up))
            {
                if (MoveUpTriggered != null) { MoveUpTriggered(); return true; }
            }
            if (keyData == (Keys.Alt | Keys.Down))
            {
                if (MoveDownTriggered != null) { MoveDownTriggered(); return true; }
            }

            // 3. Ctrl + C (Copy Password)
            if (keyData == (Keys.Control | Keys.C))
            {
                // Only copy password if user is not currently selecting text in the search input box
                if (!isSearchFocused || searchSelectionLen == 0)
                {
                    if (CopyPasswordTriggered != null) { CopyPasswordTriggered(); return true; }
                }
            }

            // 4. Productivity Hotkeys
            if (keyData == (Keys.Control | Keys.F))
            {
                if (FocusSearchTriggered != null) { FocusSearchTriggered(); return true; }
            }
            if (keyData == (Keys.Control | Keys.S))
            {
                if (SaveCredentialTriggered != null) { SaveCredentialTriggered(); return true; }
            }
            if (keyData == (Keys.Control | Keys.N))
            {
                if (ClearFormTriggered != null) { ClearFormTriggered(); return true; }
            }
            if (keyData == (Keys.Control | Keys.P) || (keyData == Keys.Space && activeCtrl is ListView))
            {
                if (TogglePasswordTriggered != null) { TogglePasswordTriggered(); return true; }
            }
            if (keyData == (Keys.Control | Keys.G))
            {
                if (GeneratePasswordTriggered != null) { GeneratePasswordTriggered(); return true; }
            }
            if (keyData == (Keys.Control | Keys.Oemcomma) || keyData == (Keys.Control | Keys.OemPeriod))
            {
                if (OpenSettingsTriggered != null) { OpenSettingsTriggered(); return true; }
            }
            if (keyData == Keys.F5)
            {
                if (RefreshTriggered != null) { RefreshTriggered(); return true; }
            }

            // 5. Delete in List View
            if (keyData == Keys.Delete && activeCtrl is ListView)
            {
                if (DeleteTriggered != null) { DeleteTriggered(); return true; }
            }

            // 6. Enter Key: Jump to Edit Selected Item
            if (keyData == Keys.Enter)
            {
                if (isSearchFocused || activeCtrl is ListView)
                {
                    if (EnterEditTriggered != null && hasListItems)
                    {
                        EnterEditTriggered();
                        return true;
                    }
                }
            }

            // 7. Down Arrow: Search Box -> List Navigation
            if (keyData == Keys.Down)
            {
                if (isSearchFocused && hasListItems)
                {
                    if (DownToNavigateListTriggered != null)
                    {
                        DownToNavigateListTriggered();
                        return true;
                    }
                }
            }

            // 8. Up Arrow: Top of List -> Search Box Focus
            if (keyData == Keys.Up && activeCtrl is ListView)
            {
                if (isTopItemSelected)
                {
                    if (UpToNavigateSearchTriggered != null)
                    {
                        UpToNavigateSearchTriggered();
                        return true;
                    }
                }
            }

            return false;
        }

        public void ResetDebouncer()
        {
            escapeDebouncer.Reset();
        }
    }
}
