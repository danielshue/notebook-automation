"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.IndexOptionsModal = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = require("react");
/**
 * Modal UI for selecting index generation mode (current folder or recursive), with feedback.
 */
const IndexOptionsModal = ({ onSelect, onClose }) => {
    const [message, setMessage] = (0, react_1.useState)(null);
    const [error, setError] = (0, react_1.useState)(null);
    const handleSelect = (mode) => __awaiter(void 0, void 0, void 0, function* () {
        setMessage(null);
        setError(null);
        try {
            yield onSelect(mode);
            setMessage(`Index generated (${mode})`);
        }
        catch (e) {
            setError(`Failed to generate index: ${e.message}`);
        }
    });
    return ((0, jsx_runtime_1.jsxs)("div", { className: "index-options-modal", children: [(0, jsx_runtime_1.jsx)("h2", { children: "Generate Index" }), message && (0, jsx_runtime_1.jsx)("div", { className: "success-message", children: message }), error && (0, jsx_runtime_1.jsx)("div", { className: "error-message", children: error }), (0, jsx_runtime_1.jsx)("p", { children: "Select how you want to generate the index:" }), (0, jsx_runtime_1.jsx)("button", { onClick: () => handleSelect('current'), children: "Current Folder Only" }), (0, jsx_runtime_1.jsx)("button", { onClick: () => handleSelect('recursive'), children: "All Subfolders (Recursive)" }), (0, jsx_runtime_1.jsx)("button", { onClick: onClose, children: "Cancel" })] }));
};
exports.IndexOptionsModal = IndexOptionsModal;
