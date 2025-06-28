

import React, { useState } from 'react';

export interface IndexOptionsModalProps {
  onSelect: (mode: 'current' | 'recursive') => Promise<void>;
  onClose: () => void;
}

/**
 * Modal UI for selecting index generation mode (current folder or recursive), with feedback.
 */
export const IndexOptionsModal: React.FC<IndexOptionsModalProps> = ({ onSelect, onClose }) => {
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSelect = async (mode: 'current' | 'recursive') => {
    setMessage(null); setError(null);
    try {
      await onSelect(mode);
      setMessage(`Index generated (${mode})`);
    } catch (e: any) {
      setError(`Failed to generate index: ${e.message}`);
    }
  };

  return (
    <div className="index-options-modal">
      <h2>Generate Index</h2>
      {message && <div className="success-message">{message}</div>}
      {error && <div className="error-message">{error}</div>}
      <p>Select how you want to generate the index:</p>
      <button onClick={() => handleSelect('current')}>Current Folder Only</button>
      <button onClick={() => handleSelect('recursive')}>All Subfolders (Recursive)</button>
      <button onClick={onClose}>Cancel</button>
    </div>
  );
};
