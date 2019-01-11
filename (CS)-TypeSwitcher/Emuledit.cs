using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static System.Math;
using static _CS__TypeSwitcher.WinAPI;

namespace _CS__TypeSwitcher {
    class Symbol {
        int keyCode;
        bool upperCase;
        public Symbol(int keyCode, bool upperCase) {
            this.keyCode = keyCode;
            this.upperCase = upperCase;
        }
        public int GetKeyCode() {
            return keyCode;
        }
        public bool isUpper() {
            return upperCase;
        }
    }
    class Emuledit {
        private List<Symbol> content;
        private int startSelection;
        private int endSelection;
        enum Actions { Retype }
        enum KeyClass { NavKey, SymbolKey, SystemKey }
        enum States { Idle, Selecting, Selected }
        States state;
        Keys[] SystemKeys = new Keys[] {
            Keys.CapsLock,
            Keys.Back,
            Keys.Return,
            Keys.LShiftKey,
            Keys.RShiftKey,
            Keys.LControlKey,
            Keys.RControlKey,
            Keys.Alt,
            Keys.Escape,
            Keys.Delete,
            Keys.LWin,
            Keys.RWin
        };
        Keys[] NavKeys = new Keys[] {
            Keys.Left,
            Keys.Right,
            Keys.Up,
            Keys.Down,
            Keys.Home,
            Keys.End,
            Keys.PageUp,
            Keys.PageDown
        };
        public Emuledit() {
            state = States.Idle;
            startSelection = 0;
            endSelection = 0;
            content = new List<Symbol>();
        }
        KeyClass GetKeyClass(int keyCode) {
            if (SystemKeys.Contains((Keys)keyCode))
                return KeyClass.SystemKey;
            if (NavKeys.Contains((Keys)keyCode))
                return KeyClass.NavKey;
            return KeyClass.SymbolKey;
        }
        public void Reset() {
            content.Clear();
            startSelection = endSelection = 0;
            state = States.Idle;
        }
        public bool ProcessKeyPress(int keyCode, KeyState keyState) {
            bool eatKey = false;
            switch (keyState) {
                case KeyState.Down:
                    switch (GetKeyClass(keyCode)) {
                        case KeyClass.NavKey:
                            switch ((Keys)keyCode) {
                                case Keys.Left:
                                    switch (state) {
                                        case States.Idle:
                                            startSelection--;
                                            startSelection = Max(startSelection, 0);
                                            endSelection = startSelection;
                                            break;
                                        case States.Selecting:
                                            endSelection--;
                                            endSelection = Max(endSelection, 0);
                                            break;
                                        case States.Selected:
                                            NormalizePointersLR();
                                            endSelection = startSelection;
                                            state = States.Idle;
                                            break;
                                    }
                                    break;
                                case Keys.Right:
                                    switch (state) {
                                        case States.Idle:
                                            startSelection++;
                                            startSelection = Min(startSelection, content.Count);
                                            endSelection = startSelection;
                                            break;
                                        case States.Selecting:
                                            endSelection++;
                                            endSelection = Min(endSelection, content.Count);
                                            break;
                                        case States.Selected:
                                            NormalizePointersRL();
                                            endSelection = startSelection;
                                            state = States.Idle;
                                            break;
                                    }
                                    break;
                                case Keys.Home:
                                    switch (state) {
                                        case States.Idle:
                                            startSelection = endSelection = 0;
                                            break;
                                        case States.Selecting:
                                            endSelection = 0;
                                            break;
                                        case States.Selected:
                                            startSelection = endSelection = 0;
                                            state = States.Idle;
                                            break;
                                    }
                                    break;
                                case Keys.End:
                                    switch (state) {
                                        case States.Idle:
                                            startSelection = endSelection = content.Count;
                                            break;
                                        case States.Selecting:
                                            endSelection = content.Count;
                                            break;
                                        case States.Selected:
                                            startSelection = endSelection = content.Count;
                                            state = States.Idle;
                                            break;
                                    }
                                    break;
                                default:
                                    Reset();
                                    break;
                            }
                            break;
                        case KeyClass.SystemKey:
                            switch ((Keys)keyCode) {
                                case Keys.Back:
                                    switch (state) {
                                        case States.Idle:
                                            if (startSelection > 0) {
                                                content.RemoveAt(startSelection - 1);
                                                startSelection--;
                                                endSelection--;
                                            }
                                            break;
                                        case States.Selecting:
                                            NormalizePointersLR();
                                            content.RemoveRange(startSelection, endSelection - startSelection);
                                            endSelection = startSelection;
                                            break;
                                        case States.Selected:
                                            NormalizePointersLR();
                                            content.RemoveRange(startSelection, endSelection - startSelection);
                                            endSelection = startSelection;
                                            state = States.Idle;
                                            break;
                                    }
                                    break;
                                case Keys.Delete:
                                    switch (state) {
                                        case States.Idle:
                                            if (startSelection < content.Count) {
                                                content.RemoveAt(startSelection);
                                            }
                                            break;
                                        case States.Selecting:
                                            NormalizePointersLR();
                                            content.RemoveRange(startSelection, endSelection - startSelection);
                                            endSelection = startSelection;
                                            break;
                                        case States.Selected:
                                            NormalizePointersLR();
                                            content.RemoveRange(startSelection, endSelection - startSelection);
                                            endSelection = startSelection;
                                            state = States.Idle;
                                            break;
                                    }
                                    break;
                                //MAIN RETYPE PROCEDURE
                                case Keys.CapsLock:
                                    switch (state) {
                                        case States.Idle:
                                            for (int i = 0; i < startSelection; i++)
                                                ClickKey((int)Keys.Back);
                                            PostMessage(GetForegroundWindow(), WM_INPUTLANGCHANGEREQUEST, 2, 0);
                                            Retype(0, startSelection);
                                            break;
                                        case States.Selected:
                                            NormalizePointersLR();
                                            ClickKey((int)Keys.Back);
                                            PostMessage(GetForegroundWindow(), WM_INPUTLANGCHANGEREQUEST, 2, 0);
                                            Retype(startSelection, endSelection);
                                            startSelection = endSelection;
                                            state = States.Idle;
                                            break;
                                    }
                                    eatKey = true;
                                    break;
                                case Keys.RShiftKey:
                                case Keys.LShiftKey:
                                    state = States.Selecting;
                                    break;
                                default:
                                    Reset();
                                    break;
                            }
                            break;
                        default:
                            //USUAL KEY
                            switch (state) {
                                case States.Idle:
                                    content.Insert(startSelection, new Symbol(keyCode, isUpperCase()));
                                    startSelection++;
                                    endSelection++;
                                    break;
                                case States.Selecting:
                                    NormalizePointersLR();
                                    content.RemoveRange(startSelection, endSelection - startSelection);
                                    content.Insert(startSelection, new Symbol(keyCode, isUpperCase()));
                                    endSelection = ++startSelection;
                                    break;               //OPTIMIZE
                                case States.Selected:
                                    NormalizePointersLR();
                                    content.RemoveRange(startSelection, endSelection - startSelection);
                                    content.Insert(startSelection, new Symbol(keyCode, isUpperCase()));
                                    endSelection = ++startSelection;
                                    state = States.Idle; //OPTIMIZE
                                    break;
                            }
                            break;
                    }
                    break;
                case KeyState.Up:
                    switch (GetKeyClass(keyCode)) {
                        case KeyClass.SystemKey:
                            switch ((Keys)keyCode) {
                                case Keys.LShiftKey:
                                case Keys.RShiftKey:
                                    if (startSelection != endSelection)
                                        state = States.Selected;
                                    else
                                        state = States.Idle;
                                    break;
                            }
                            break;
                    }
                    break;
            }
            Console.WriteLine(state);
            return eatKey;
        }
        private void NormalizePointersLR() {
            if (startSelection > endSelection)
                Swap(ref startSelection, ref endSelection);
        }
        private void NormalizePointersRL() {
            if (startSelection < endSelection)
                Swap(ref startSelection, ref endSelection);
        }
        private void GoTo(int pos) {
            void GoTo(int from, int to) {
                int dist = to - from;
                int direction = Sign(dist);
                for (int i = 0; i < dist; i++) {
                    if (direction == -1)
                        ClickKey((int)Keys.Left);
                    else
                        ClickKey((int)Keys.Right);
                }
                startSelection = endSelection = to;
            }
            if (pos < 0 && pos > content.Count)
                return;
            switch (state) {
                case States.Idle:
                    GoTo(startSelection, pos);
                    break;
                case States.Selected:
                    NormalizePointersLR();
                    if (Abs(startSelection - pos) < Abs(endSelection - pos)) {
                        ClickKey((int)Keys.Left);
                        GoTo(startSelection, pos);
                    }
                    else {
                        ClickKey((int)Keys.Right);
                        GoTo(endSelection, pos);
                    }
                    state = States.Idle;
                    break;
            }
        }
        //retype in range [start, end)
        private void Retype(int start, int end) {
            for (; start < end; start++) {
                Symbol symb = content[start];
                if (symb.isUpper())
                    ClickKeysTogether(new List<int> { (int)Keys.LShiftKey, symb.GetKeyCode() });
                else
                    ClickKey((short)symb.GetKeyCode());
            }
        }
        public void PrintContent() {
            foreach (Symbol s in content)
                Console.Write((Keys)s.GetKeyCode());
            Console.WriteLine();
        }
    }
}
