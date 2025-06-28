

import React, { useState } from 'react';

export interface ResourcePanelProps {
  files: string[];
  onLaunch: (file: string) => Promise<void>;
  onImport: (file: string) => Promise<void>;
}

/**
 * UI panel for displaying resource files and actions (launch/import), with feedback.
 */
export const ResourcePanel: React.FC<ResourcePanelProps> = ({ files, onLaunch, onImport }) => {
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleLaunch = async (file: string) => {
    setMessage(null); setError(null);
    try {
      await onLaunch(file);
      setMessage(`Opened ${file}`);
    } catch (e: any) {
      setError(`Failed to open ${file}: ${e.message}`);
    }
  };

  const handleImport = async (file: string) => {
    setMessage(null); setError(null);
    try {
      await onImport(file);
      setMessage(`Imported ${file}`);
    } catch (e: any) {
      setError(`Failed to import ${file}: ${e.message}`);
    }
  };

  return (
    <div className="resource-panel">
      <h2>Resource Files</h2>
      {message && <div className="success-message">{message}</div>}
      {error && <div className="error-message">{error}</div>}
      {files.length === 0 ? (
        <p>No resource files found.</p>
      ) : (
        <ul>
          {files.map(file => (
            <li key={file}>
              {file}
              <button onClick={() => handleLaunch(file)}>Open</button>
              <button onClick={() => handleImport(file)}>Import</button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};
