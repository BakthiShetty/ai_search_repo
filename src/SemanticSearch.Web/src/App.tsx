import { useState, useMemo, useEffect } from 'react';
import { useSearch } from './hooks/use-search';
import type { SearchResult } from './hooks/use-search';
import SearchIcon from '@mui/icons-material/Search';
import FolderIcon from '@mui/icons-material/Folder';
import FolderOpenIcon from '@mui/icons-material/FolderOpen';
import InsertDriveFileIcon from '@mui/icons-material/InsertDriveFile';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import ArrowDropDownIcon from '@mui/icons-material/ArrowDropDown';
import UnfoldMoreIcon from '@mui/icons-material/UnfoldMore';
import CloseIcon from '@mui/icons-material/Close';
import DownloadIcon from '@mui/icons-material/Download';
import CircularProgress from '@mui/material/CircularProgress';
import './App.css';

// ── Types ────────────────────────────────────────────────────────────────────

interface FolderNode {
  name: string;
  path: string;
  children: FolderNode[];
  fileCount: number;
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function buildFolderTree(results: SearchResult[]): FolderNode[] {
  const nodeMap = new Map<string, FolderNode>();
  const root: FolderNode[] = [];

  for (const r of results) {
    if (!r.folderPath) continue;
    const parts = r.folderPath.replace(/\\/g, '/').replace(/^\//, '').split('/').filter(Boolean);
    let parentChildren = root;
    let currentPath = '';

    parts.forEach((part, idx) => {
      currentPath = currentPath ? `${currentPath}/${part}` : part;
      if (!nodeMap.has(currentPath)) {
        const node: FolderNode = { name: part, path: currentPath, children: [], fileCount: 0 };
        nodeMap.set(currentPath, node);
        parentChildren.push(node);
      }
      const node = nodeMap.get(currentPath)!;
      if (idx === parts.length - 1) node.fileCount++;
      parentChildren = node.children;
    });
  }

  return root;
}

const EXT_COLORS: Record<string, string> = {
  pdf: '#dc2626', docx: '#2563eb', doc: '#2563eb',
  xlsx: '#16a34a', xls: '#16a34a', csv: '#16a34a',
  pptx: '#ea580c', ppt: '#ea580c',
  txt: '#6b7280', md: '#6b7280',
  dwg: '#7c3aed', dxf: '#7c3aed', ifc: '#0891b2',
  zip: '#854d0e', rar: '#854d0e',
  jpg: '#db2777', jpeg: '#db2777', png: '#db2777',
};

const PREVIEWABLE_EXTS = new Set(['pdf', 'txt', 'md']);

function getExt(filename: string) {
  return filename.split('.').pop()?.toLowerCase() ?? 'file';
}

function formatDate(s: string | null): string {
  if (!s) return '—';
  try {
    return new Date(s).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  } catch {
    return s;
  }
}

// ── ScoreBadge ────────────────────────────────────────────────────────────────

function ScoreBadge({ score }: { score: number }) {
  const pct = Math.round(score * 100);
  const [color, bg] =
    pct >= 75 ? ['#16a34a', '#dcfce7'] :
    pct >= 50 ? ['#d97706', '#fef3c7'] :
    ['#6b7280', '#f3f4f6'];
  return (
    <span className="score-badge" style={{ color, backgroundColor: bg }}>
      {pct}%
    </span>
  );
}

// ── FolderTreeItem ────────────────────────────────────────────────────────────

interface FolderTreeItemProps {
  node: FolderNode;
  depth: number;
  selectedPath: string | null;
  onSelect: (path: string | null) => void;
}

function FolderTreeItem({ node, depth, selectedPath, onSelect }: FolderTreeItemProps) {
  const [expanded, setExpanded] = useState(depth === 0);
  const isSelected = selectedPath === node.path;

  return (
    <div>
      <div
        className={`folder-item${isSelected ? ' selected' : ''}`}
        style={{ paddingLeft: `${10 + depth * 14}px` }}
        onClick={() => {
          onSelect(isSelected ? null : node.path);
          if (node.children.length > 0) setExpanded(e => !e);
        }}
      >
        <span className="folder-toggle">
          {node.children.length > 0 ? (expanded ? '▾' : '▸') : ''}
        </span>
        {isSelected
          ? <FolderOpenIcon className="folder-icon open" sx={{ fontSize: 16 }} />
          : <FolderIcon className="folder-icon" sx={{ fontSize: 16 }} />
        }
        <span className="folder-name">{node.name}</span>
        {node.fileCount > 0 && (
          <span className="folder-count">{node.fileCount}</span>
        )}
      </div>
      {expanded && node.children.map(child => (
        <FolderTreeItem
          key={child.path}
          node={child}
          depth={depth + 1}
          selectedPath={selectedPath}
          onSelect={onSelect}
        />
      ))}
    </div>
  );
}

// ── PreviewPanel ──────────────────────────────────────────────────────────────

interface PreviewPanelProps {
  file: SearchResult;
  onClose: () => void;
}

function PreviewPanel({ file, onClose }: PreviewPanelProps) {
  const ext = getExt(file.filename);
  const extColor = EXT_COLORS[ext] ?? '#6b7280';
  const canPreview = PREVIEWABLE_EXTS.has(ext);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [onClose]);

  return (
    <>
      <div className="preview-backdrop" onClick={onClose} />
      <aside className="preview-panel">
        <div className="preview-header">
          <div className="preview-title">
            <span className="ext-badge" style={{ background: extColor }}>
              {ext.toUpperCase()}
            </span>
            <span className="preview-filename" title={file.filename}>
              {file.filename}
            </span>
          </div>
          <div className="preview-header-actions">
            <a
              href={`/api/files/${file.id}?download=true`}
              download={file.filename}
              className="btn-teal sm"
            >
              <DownloadIcon sx={{ fontSize: 13 }} />
              Download
            </a>
            <button className="preview-close" onClick={onClose} aria-label="Close preview">
              <CloseIcon sx={{ fontSize: 17 }} />
            </button>
          </div>
        </div>

        <div className="preview-meta">
          {file.folderPath && (
            <span className="pmeta-item">
              <FolderIcon sx={{ fontSize: 12 }} />
              {file.folderPath}
            </span>
          )}
          {file.author && <span className="pmeta-item pmeta-author">{file.author}</span>}
          {file.createdAt && (
            <span className="pmeta-item">{formatDate(file.createdAt)}</span>
          )}
          <ScoreBadge score={file.score} />
        </div>

        <div className="preview-body">
          {canPreview ? (
            <iframe
              key={file.id}
              src={`/api/files/${file.id}`}
              title={file.filename}
              className="preview-iframe"
            />
          ) : (
            <div className="preview-unsupported">
              <InsertDriveFileIcon sx={{ fontSize: 48, opacity: 0.18 }} />
              <p className="preview-unsupported-msg">
                Preview not available for <strong>.{ext}</strong> files
              </p>
              <p className="preview-unsupported-sub">
                Download the file to open it in the appropriate application.
              </p>
              <a
                href={`/api/files/${file.id}?download=true`}
                download={file.filename}
                className="btn-teal sm"
              >
                <DownloadIcon sx={{ fontSize: 13 }} />
                Download to open
              </a>
            </div>
          )}
        </div>
      </aside>
    </>
  );
}

// ── App ───────────────────────────────────────────────────────────────────────

export default function App() {
  const { query, setQuery, results, loading, error, searched } = useSearch();
  const [selectedFolder, setSelectedFolder] = useState<string | null>(null);
  const [folderSearch, setFolderSearch] = useState('');
  const [previewFile, setPreviewFile] = useState<SearchResult | null>(null);

  const folderTree = useMemo(() => buildFolderTree(results), [results]);

  const displayedResults = useMemo(() => {
    let list = results;
    if (selectedFolder) {
      list = list.filter(r =>
        r.folderPath?.replace(/\\/g, '/').includes(selectedFolder) ?? false
      );
    }
    if (folderSearch.trim()) {
      const q = folderSearch.toLowerCase();
      list = list.filter(r => r.filename.toLowerCase().includes(q));
    }
    return list;
  }, [results, selectedFolder, folderSearch]);

  const breadcrumb = selectedFolder
    ? selectedFolder.split('/').join(' / ')
    : searched ? 'Search Results' : 'All Documents';

  return (
    <div className="app-root">

      {/* ── Top Navigation ─────────────────────────────────────────── */}
      <nav className="top-nav">
        <span className="brand">AjourBox</span>

        <div className="nav-tabs">
          <button className="nav-tab">
            File review <span className="tab-badge">0</span>
          </button>
          <button className="nav-tab">
            Recycle Bin <span className="tab-badge">82</span>
          </button>
        </div>

        <div className="global-search-field">
          <span className="gsearch-icon">
            {loading
              ? <CircularProgress size={13} sx={{ color: 'rgba(255,255,255,0.55)' }} />
              : <SearchIcon sx={{ fontSize: 15, color: 'rgba(255,255,255,0.55)' }} />
            }
          </span>
          <input
            type="text"
            className="gsearch-input"
            placeholder="Global search"
            value={query}
            onChange={e => { setQuery(e.target.value); setSelectedFolder(null); }}
          />
          {query && results.length > 0 && (
            <span className="gsearch-count">{results.length}</span>
          )}
          {query && (
            <button
              className="gsearch-clear"
              onClick={() => { setQuery(''); setSelectedFolder(null); setFolderSearch(''); }}
            >
              ×
            </button>
          )}
        </div>
      </nav>

      <div className="workspace">

        {/* ── Left Sidebar ───────────────────────────────────────────── */}
        <aside className="sidebar">
          <div className="sidebar-top">
            <button className="btn-outline sm">
              Menu <ArrowDropDownIcon sx={{ fontSize: 15 }} />
            </button>
            <button className="btn-teal sm">
              0000000 <ArrowDropDownIcon sx={{ fontSize: 15 }} />
            </button>
          </div>

          <div className="sidebar-tree">
            {!searched && (
              <div className="tree-placeholder">
                <FolderIcon sx={{ fontSize: 30, opacity: 0.22 }} />
                <p>Use the global search to explore document folders</p>
              </div>
            )}

            {searched && folderTree.length > 0 && (
              <>
                <p className="tree-section-label">Folders on project</p>

                <div
                  className={`tree-all-files${selectedFolder === null ? ' active' : ''}`}
                  onClick={() => setSelectedFolder(null)}
                >
                  <InsertDriveFileIcon sx={{ fontSize: 14, color: '#8a95a3' }} />
                  <span className="folder-name">All Files</span>
                  <span className="folder-count">{results.length}</span>
                </div>

                {folderTree.map(node => (
                  <FolderTreeItem
                    key={node.path}
                    node={node}
                    depth={0}
                    selectedPath={selectedFolder}
                    onSelect={setSelectedFolder}
                  />
                ))}
              </>
            )}

            {searched && folderTree.length === 0 && results.length > 0 && (
              <div className="tree-placeholder">
                <FolderIcon sx={{ fontSize: 26, opacity: 0.22 }} />
                <p>No folder structure in results</p>
              </div>
            )}
          </div>
        </aside>

        {/* ── Main Pane ──────────────────────────────────────────────── */}
        <main className="main-pane">

          {/* Content toolbar */}
          <div className="content-toolbar">
            <button className="btn-outline sm">
              Menu <ArrowDropDownIcon sx={{ fontSize: 15 }} />
            </button>
            <button className="btn-teal sm upload-btn">
              <CloudUploadIcon sx={{ fontSize: 14 }} />
              Upload files
            </button>
            <div className="breadcrumb">
              <span className="bc-label">YOU ARE HERE:</span>
              <span className="bc-path">/ {breadcrumb}</span>
            </div>
            <div className="toolbar-fill" />
            <button className="btn-teal sm">
              Combined <ArrowDropDownIcon sx={{ fontSize: 15 }} />
            </button>
          </div>

          {/* Stats bar */}
          <div className="stats-bar">
            <span className="stats-text">
              {searched
                ? <>Showing {displayedResults.length} of {results.length} files &nbsp;
                    <span className="sel-badge">0 file(s) selected</span>
                  </>
                : <>Showing 0 to 0 of 0 files &nbsp;
                    <span className="sel-badge">0 file(s) selected</span>
                  </>
              }
            </span>
            <div className="stats-right">
              <label className="show-label">
                Show
                <select className="show-select" defaultValue="10">
                  <option value="10">10</option>
                  <option value="25">25</option>
                  <option value="50">50</option>
                  <option value="100">100</option>
                </select>
                files
              </label>
              <div className="folder-search">
                <SearchIcon sx={{ fontSize: 13, color: '#9aa3b0' }} />
                <input
                  type="text"
                  placeholder="Search in folder"
                  value={folderSearch}
                  onChange={e => setFolderSearch(e.target.value)}
                />
              </div>
            </div>
          </div>

          {/* File table */}
          <div className="table-wrap">
            <table className="file-table">
              <thead>
                <tr>
                  <th className="col-name">Name <UnfoldMoreIcon className="sort-ic" /></th>
                  <th className="col-desc">Description <UnfoldMoreIcon className="sort-ic" /></th>
                  <th className="col-rev">Revision <UnfoldMoreIcon className="sort-ic" /></th>
                  <th className="col-ver">Version <UnfoldMoreIcon className="sort-ic" /></th>
                  <th className="col-size">Size <UnfoldMoreIcon className="sort-ic" /></th>
                  <th className="col-date">Updated <UnfoldMoreIcon className="sort-ic" /></th>
                  <th className="col-by">Updated by <UnfoldMoreIcon className="sort-ic" /></th>
                </tr>
              </thead>
              <tbody>
                {error && (
                  <tr>
                    <td colSpan={7} className="td-empty td-error">⚠ {error}</td>
                  </tr>
                )}

                {!error && !searched && !loading && (
                  <tr>
                    <td colSpan={7} className="td-empty">No files in folder...</td>
                  </tr>
                )}

                {!error && searched && displayedResults.length === 0 && !loading && (
                  <tr>
                    <td colSpan={7} className="td-empty">No files found for this query.</td>
                  </tr>
                )}

                {displayedResults.map((r, i) => {
                  const ext = getExt(r.filename);
                  const extColor = EXT_COLORS[ext] ?? '#6b7280';
                  const isActive = previewFile?.id === r.id;
                  return (
                    <tr
                      key={r.id}
                      className={[
                        i % 2 !== 0 ? 'tr-alt' : '',
                        isActive ? 'tr-active' : '',
                      ].join(' ').trim()}
                    >
                      <td className="col-name">
                        <div className="fname-cell">
                          <span className="ext-badge" style={{ background: extColor }}>
                            {ext.toUpperCase()}
                          </span>
                          <button
                            className="fname-text"
                            onClick={() => setPreviewFile(isActive ? null : r)}
                            title={r.nasPath || r.filename}
                          >
                            {r.filename}
                          </button>
                          <ScoreBadge score={r.score} />
                        </div>
                      </td>
                      <td className="col-desc td-muted">{r.folderPath ?? '—'}</td>
                      <td className="td-muted">—</td>
                      <td className="td-muted">—</td>
                      <td className="td-muted">—</td>
                      <td className="td-muted">{formatDate(r.createdAt)}</td>
                      <td className="td-muted">{r.author ?? '—'}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </main>
      </div>

      {/* ── File Preview Panel ─────────────────────────────────────── */}
      {previewFile && (
        <PreviewPanel
          file={previewFile}
          onClose={() => setPreviewFile(null)}
        />
      )}
    </div>
  );
}
