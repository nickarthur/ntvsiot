//*********************************************************//
//    Copyright (c) Microsoft. All rights reserved.
//    
//    Apache 2.0 License
//    
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing, software 
//    distributed under the License is distributed on an "AS IS" BASIS, 
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or 
//    implied. See the License for the specific language governing 
//    permissions and limitations under the License.
//
//*********************************************************//

using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudioTools.Navigation {
    internal class TextLineEventListener : IVsTextLinesEvents, IDisposable {
        private const int _defaultDelay = 2000;
        private string _fileName;
        private ModuleId _fileId;
        private IVsTextLines _buffer;
        private bool _isDirty;
        private IConnectionPoint _connectionPoint;
        private uint _connectionCookie;

        public TextLineEventListener(IVsTextLines buffer, string fileName, ModuleId id) {
            _buffer = buffer;
            _fileId = id;
            _fileName = fileName;
            IConnectionPointContainer container = buffer as IConnectionPointContainer;
            if (null != container) {
                Guid eventsGuid = typeof(IVsTextLinesEvents).GUID;
                container.FindConnectionPoint(ref eventsGuid, out _connectionPoint);
                _connectionPoint.Advise(this as IVsTextLinesEvents, out _connectionCookie);
            }
        }

        #region Properties
        public ModuleId FileID {
            get { return _fileId; }
        }
        public string FileName {
            get { return _fileName; }
            set { _fileName = value; }
        }
        #endregion

        #region Events
        public event EventHandler<HierarchyEventArgs> OnFileChanged;

        public event TextLineChangeEvent OnFileChangedImmediate;

        #endregion

        #region IVsTextLinesEvents Members
        void IVsTextLinesEvents.OnChangeLineAttributes(int iFirstLine, int iLastLine) {
            // Do Nothing
        }

        void IVsTextLinesEvents.OnChangeLineText(TextLineChange[] pTextLineChange, int fLast) {
            TextLineChangeEvent eh = OnFileChangedImmediate;
            if (null != eh) {
                eh(this, pTextLineChange, fLast);
            }

            _isDirty = true;
        }
        #endregion

        #region IDisposable Members
        public void Dispose() {
            if ((null != _connectionPoint) && (0 != _connectionCookie)) {
                _connectionPoint.Unadvise(_connectionCookie);
            }
            _connectionCookie = 0;
            _connectionPoint = null;

            _buffer = null;
            _fileId = null;
        }
        #endregion

        #region Idle time processing
        public void OnIdle() {
            if (!_isDirty) {
                return;
            }
            var onFileChanged = OnFileChanged;
            if (null != onFileChanged) {
                HierarchyEventArgs args = new HierarchyEventArgs(_fileId.ItemID, _fileName);
                args.TextBuffer = _buffer;
                onFileChanged(_fileId.Hierarchy, args);
            }

            _isDirty = false;
        }
        #endregion
    }
}
