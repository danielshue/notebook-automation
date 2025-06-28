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
exports.ResourcePanel = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = require("react");
/**
 * UI panel for displaying resource files and actions (launch/import), with feedback.
 */
const ResourcePanel = ({ files, onLaunch, onImport }) => {
    const [message, setMessage] = (0, react_1.useState)(null);
    const [error, setError] = (0, react_1.useState)(null);
    const handleLaunch = (file) => __awaiter(void 0, void 0, void 0, function* () {
        setMessage(null);
        setError(null);
        try {
            yield onLaunch(file);
            setMessage(`Opened ${file}`);
        }
        catch (e) {
            setError(`Failed to open ${file}: ${e.message}`);
        }
    });
    const handleImport = (file) => __awaiter(void 0, void 0, void 0, function* () {
        setMessage(null);
        setError(null);
        try {
            yield onImport(file);
            setMessage(`Imported ${file}`);
        }
        catch (e) {
            setError(`Failed to import ${file}: ${e.message}`);
        }
    });
    return ((0, jsx_runtime_1.jsxs)("div", { className: "resource-panel", children: [(0, jsx_runtime_1.jsx)("h2", { children: "Resource Files" }), message && (0, jsx_runtime_1.jsx)("div", { className: "success-message", children: message }), error && (0, jsx_runtime_1.jsx)("div", { className: "error-message", children: error }), files.length === 0 ? ((0, jsx_runtime_1.jsx)("p", { children: "No resource files found." })) : ((0, jsx_runtime_1.jsx)("ul", { children: files.map(file => ((0, jsx_runtime_1.jsxs)("li", { children: [file, (0, jsx_runtime_1.jsx)("button", { onClick: () => handleLaunch(file), children: "Open" }), (0, jsx_runtime_1.jsx)("button", { onClick: () => handleImport(file), children: "Import" })] }, file))) }))] }));
};
exports.ResourcePanel = ResourcePanel;
