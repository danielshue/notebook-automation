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
exports.IndexOptionsModal = void 0;
const react_1 = __importStar(require("react"));
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
    return (react_1.default.createElement("div", { className: "index-options-modal" },
        react_1.default.createElement("h2", null, "Generate Index"),
        message && react_1.default.createElement("div", { className: "success-message" }, message),
        error && react_1.default.createElement("div", { className: "error-message" }, error),
        react_1.default.createElement("p", null, "Select how you want to generate the index:"),
        react_1.default.createElement("button", { onClick: () => handleSelect('current') }, "Current Folder Only"),
        react_1.default.createElement("button", { onClick: () => handleSelect('recursive') }, "All Subfolders (Recursive)"),
        react_1.default.createElement("button", { onClick: onClose }, "Cancel")));
};
exports.IndexOptionsModal = IndexOptionsModal;
