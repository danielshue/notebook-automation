"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
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
const react_1 = __importStar(require("react"));
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
    return (react_1.default.createElement("div", { className: "resource-panel" },
        react_1.default.createElement("h2", null, "Resource Files"),
        message && react_1.default.createElement("div", { className: "success-message" }, message),
        error && react_1.default.createElement("div", { className: "error-message" }, error),
        files.length === 0 ? (react_1.default.createElement("p", null, "No resource files found.")) : (react_1.default.createElement("ul", null, files.map(file => (react_1.default.createElement("li", { key: file },
            file,
            react_1.default.createElement("button", { onClick: () => handleLaunch(file) }, "Open"),
            react_1.default.createElement("button", { onClick: () => handleImport(file) }, "Import"))))))));
};
exports.ResourcePanel = ResourcePanel;
