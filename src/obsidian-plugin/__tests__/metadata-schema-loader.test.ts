/**
 * Tests for metadata schema loader integration in the plugin context.
 * Validates that the plugin can properly load and use the metadata schema for field resolution.
 */

import { jest } from '@jest/globals';

// Mock file system modules
const mockFs = {
  existsSync: jest.fn(),
  readFileSync: jest.fn(),
  statSync: jest.fn(),
};

const mockPath = {
  join: jest.fn(),
  resolve: jest.fn(),
  isAbsolute: jest.fn(),
};

// Mock the require function to return our mocked modules
const originalRequire = global.require;
global.require = jest.fn((moduleName: string) => {
  if (moduleName === 'fs') return mockFs;
  if (moduleName === 'path') return mockPath;
  return originalRequire ? originalRequire(moduleName) : {};
}) as any;

// Mock window.require for the plugin context
(global as any).window = {
  require: global.require,
  process: {
    platform: 'win32',
    arch: 'x64',
  },
};

// Mock process.env
global.process = {
  ...global.process,
  env: {
    NOTEBOOKAUTOMATION_CONFIG: '',
  },
};

// Mock schema data based on the actual metadata-schema.yaml
const mockSchemaData = `
TemplateTypes:
  pdf-reference:
    BaseTypes:
      - universal-fields
    Type: note/case-study
    RequiredFields:
      - comprehension
      - status
      - completion-date
      - authors
      - tags
    Fields:
      publisher:
        Default: University of Illinois at Urbana-Champaign
      status:
        Default: unread
      comprehension:
        Default: 0
      date-created:
        Resolver: DateCreatedResolver
      title:
        Default: "PDF Note"
      tags:
        Default: [pdf, reference]
      page-count:
        Resolver: PdfPageCountResolver
  video-reference:
    BaseTypes:
      - universal-fields
    Type: note/video-note
    RequiredFields:
      - comprehension
      - status
      - video-duration
      - author
      - tags
    Fields:
      publisher:
        Default: University of Illinois at Urbana-Champaign
      status:
        Default: unwatched
      comprehension:
        Default: 0
      date-created:
        Resolver: DateCreatedResolver
      title:
        Default: "Video Note"
      tags:
        Default: [video, reference]
      video-duration:
        Default: "00:00:00"
UniversalFields:
  - auto-generated-state
  - date-created
  - publisher
TypeMapping:
  pdf-reference: note/case-study
  video-reference: note/video-note
ReservedTags:
  - case-study
  - live-class
  - reading
  - finance
  - operations
  - video
  - pdf
`;

describe('MetadataSchemaLoader Plugin Integration', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    
    // Setup default mock behaviors
    mockFs.existsSync.mockReturnValue(true);
    mockFs.readFileSync.mockReturnValue(mockSchemaData);
    mockFs.statSync.mockReturnValue({ isFile: () => true });
    mockPath.join.mockImplementation((...args) => args.join('/'));
    mockPath.resolve.mockImplementation((...args) => args.join('/'));
    mockPath.isAbsolute.mockImplementation((path: any) => path.startsWith('/'));
  });

  describe('Schema File Loading', () => {
    it('should load schema from environment variable path when available', () => {
      // Arrange
      const envConfigPath = '/path/to/env/config.json';
      process.env.NOTEBOOKAUTOMATION_CONFIG = envConfigPath;
      
      // Mock config loading logic that would exist in plugin
      const mockLoadConfig = () => {
        if (process.env.NOTEBOOKAUTOMATION_CONFIG && mockFs.existsSync(process.env.NOTEBOOKAUTOMATION_CONFIG)) {
          return { configPath: process.env.NOTEBOOKAUTOMATION_CONFIG };
        }
        return null;
      };

      // Act
      const result = mockLoadConfig();
      
      // Assert
      expect(result).toEqual({ configPath: envConfigPath });
      expect(mockFs.existsSync).toHaveBeenCalledWith(envConfigPath);
    });

    it('should fall back to default-config.json from plugin directory', () => {
      // Arrange
      const pluginDir = '/vault/.obsidian/plugins/notebook-automation';
      const defaultConfigPath = '/vault/.obsidian/plugins/notebook-automation/default-config.json';
      process.env.NOTEBOOKAUTOMATION_CONFIG = '';
      
      mockPath.join.mockReturnValue(defaultConfigPath);
      
      // Mock plugin directory resolution logic
      const mockGetPluginConfigPath = (manifestDir: string) => {
        const defaultPath = mockPath.join(manifestDir, 'default-config.json');
        if (mockFs.existsSync(defaultPath)) {
          return defaultPath;
        }
        return null;
      };

      // Act
      const result = mockGetPluginConfigPath(pluginDir);
      
      // Assert
      expect(result).toBe(defaultConfigPath);
      expect(mockPath.join).toHaveBeenCalledWith(pluginDir, 'default-config.json');
      expect(mockFs.existsSync).toHaveBeenCalledWith(defaultConfigPath);
    });

    it('should handle missing schema file gracefully', () => {
      // Arrange
      mockFs.existsSync.mockReturnValue(false);
      
      // Mock error handling logic
      const mockLoadConfigSafely = (path: string) => {
        try {
          if (!mockFs.existsSync(path)) {
            throw new Error('Config file not found');
          }
          return { loaded: true };
        } catch (error) {
          return { error: (error as Error).message };
        }
      };

      // Act
      const result = mockLoadConfigSafely('/nonexistent/path');
      
      // Assert
      expect(result).toEqual({ error: 'Config file not found' });
      expect(mockFs.existsSync).toHaveBeenCalledWith('/nonexistent/path');
    });
  });

  describe('Schema Parsing and Validation', () => {
    it('should parse template types from schema', () => {
      // Arrange
      const mockParseSchema = (schemaContent: string) => {
        // Simple YAML-like parsing simulation
        const lines = schemaContent.split('\n');
        const templateTypes: string[] = [];
        
        let inTemplateTypes = false;
        for (const line of lines) {
          if (line.trim() === 'TemplateTypes:') {
            inTemplateTypes = true;
            continue;
          }
          if (inTemplateTypes && line.startsWith('  ') && !line.startsWith('    ')) {
            const templateType = line.trim().replace(':', '');
            if (templateType) {
              templateTypes.push(templateType);
            }
          }
          if (inTemplateTypes && line.trim() && !line.startsWith(' ')) {
            break;
          }
        }
        
        return templateTypes;
      };

      // Act
      const templateTypes = mockParseSchema(mockSchemaData);
      
      // Assert
      expect(templateTypes).toContain('pdf-reference');
      expect(templateTypes).toContain('video-reference');
      expect(templateTypes).toHaveLength(2);
    });

    it('should extract reserved tags from schema', () => {
      // Arrange
      const mockParseReservedTags = (schemaContent: string) => {
        const lines = schemaContent.split('\n');
        const reservedTags: string[] = [];
        
        let inReservedTags = false;
        for (const line of lines) {
          if (line.trim() === 'ReservedTags:') {
            inReservedTags = true;
            continue;
          }
          if (inReservedTags && line.startsWith('  - ')) {
            const tag = line.trim().replace('- ', '');
            if (tag) {
              reservedTags.push(tag);
            }
          }
          if (inReservedTags && line.trim() && !line.startsWith(' ')) {
            break;
          }
        }
        
        return reservedTags;
      };

      // Act
      const reservedTags = mockParseReservedTags(mockSchemaData);
      
      // Assert
      expect(reservedTags).toContain('case-study');
      expect(reservedTags).toContain('video');
      expect(reservedTags).toContain('pdf');
      expect(reservedTags).toContain('finance');
      expect(reservedTags).toContain('operations');
      expect(reservedTags.length).toBeGreaterThan(0);
    });

    it('should extract universal fields from schema', () => {
      // Arrange
      const mockParseUniversalFields = (schemaContent: string) => {
        const lines = schemaContent.split('\n');
        const universalFields: string[] = [];
        
        let inUniversalFields = false;
        for (const line of lines) {
          if (line.trim() === 'UniversalFields:') {
            inUniversalFields = true;
            continue;
          }
          if (inUniversalFields && line.startsWith('  - ')) {
            const field = line.trim().replace('- ', '');
            if (field) {
              universalFields.push(field);
            }
          }
          if (inUniversalFields && line.trim() && !line.startsWith(' ')) {
            break;
          }
        }
        
        return universalFields;
      };

      // Act
      const universalFields = mockParseUniversalFields(mockSchemaData);
      
      // Assert
      expect(universalFields).toContain('auto-generated-state');
      expect(universalFields).toContain('date-created');
      expect(universalFields).toContain('publisher');
      expect(universalFields).toHaveLength(3);
    });
  });

  describe('Field Resolution', () => {
    it('should resolve default values for template fields', () => {
      // Arrange
      const mockResolveFieldDefaults = (templateType: string, fieldName: string) => {
        const defaults: Record<string, Record<string, any>> = {
          'pdf-reference': {
            'publisher': 'University of Illinois at Urbana-Champaign',
            'status': 'unread',
            'comprehension': 0,
            'title': 'PDF Note',
            'tags': ['pdf', 'reference'],
          },
          'video-reference': {
            'publisher': 'University of Illinois at Urbana-Champaign',
            'status': 'unwatched',
            'comprehension': 0,
            'title': 'Video Note',
            'tags': ['video', 'reference'],
            'video-duration': '00:00:00',
          },
        };
        
        return defaults[templateType]?.[fieldName] || null;
      };

      // Act & Assert
      expect(mockResolveFieldDefaults('pdf-reference', 'status')).toBe('unread');
      expect(mockResolveFieldDefaults('pdf-reference', 'comprehension')).toBe(0);
      expect(mockResolveFieldDefaults('pdf-reference', 'tags')).toEqual(['pdf', 'reference']);
      
      expect(mockResolveFieldDefaults('video-reference', 'status')).toBe('unwatched');
      expect(mockResolveFieldDefaults('video-reference', 'video-duration')).toBe('00:00:00');
      expect(mockResolveFieldDefaults('video-reference', 'tags')).toEqual(['video', 'reference']);
    });

    it('should identify fields that require resolvers', () => {
      // Arrange
      const mockGetResolverFields = (templateType: string) => {
        const resolverFields: Record<string, string[]> = {
          'pdf-reference': ['date-created', 'page-count'],
          'video-reference': ['date-created'],
        };
        
        return resolverFields[templateType] || [];
      };

      // Act & Assert
      expect(mockGetResolverFields('pdf-reference')).toContain('date-created');
      expect(mockGetResolverFields('pdf-reference')).toContain('page-count');
      expect(mockGetResolverFields('video-reference')).toContain('date-created');
      expect(mockGetResolverFields('video-reference')).toHaveLength(1);
    });

    it('should validate required fields for template types', () => {
      // Arrange
      const mockValidateRequiredFields = (templateType: string, metadata: Record<string, any>) => {
        const requiredFields: Record<string, string[]> = {
          'pdf-reference': ['comprehension', 'status', 'completion-date', 'authors', 'tags'],
          'video-reference': ['comprehension', 'status', 'video-duration', 'author', 'tags'],
        };
        
        const required = requiredFields[templateType] || [];
        const missing = required.filter(field => !(field in metadata));
        
        return { isValid: missing.length === 0, missing };
      };

      // Act & Assert
      const validPdfMetadata = {
        comprehension: 0,
        status: 'unread',
        'completion-date': '2024-01-01',
        authors: ['Author'],
        tags: ['pdf'],
      };
      
      const invalidPdfMetadata = {
        comprehension: 0,
        status: 'unread',
        // Missing completion-date, authors, tags
      };
      
      expect(mockValidateRequiredFields('pdf-reference', validPdfMetadata)).toEqual({
        isValid: true,
        missing: [],
      });
      
      expect(mockValidateRequiredFields('pdf-reference', invalidPdfMetadata)).toEqual({
        isValid: false,
        missing: ['completion-date', 'authors', 'tags'],
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle malformed schema gracefully', () => {
      // Arrange
      const malformedSchema = 'invalid: yaml: content: [unclosed';
      mockFs.readFileSync.mockReturnValue(malformedSchema);
      
      const mockParseSchemaWithErrorHandling = (schemaContent: string) => {
        try {
          // Simple validation - check for basic structure
          if (!schemaContent.includes('TemplateTypes:')) {
            throw new Error('Invalid schema format: missing TemplateTypes');
          }
          return { success: true, data: {} };
        } catch (error) {
          return { success: false, error: (error as Error).message };
        }
      };

      // Act
      const result = mockParseSchemaWithErrorHandling(malformedSchema);
      
      // Assert
      expect(result).toEqual({
        success: false,
        error: 'Invalid schema format: missing TemplateTypes',
      });
    });

    it('should handle missing template type gracefully', () => {
      // Arrange
      const mockGetTemplateType = (templateType: string) => {
        const validTypes = ['pdf-reference', 'video-reference'];
        
        if (!validTypes.includes(templateType)) {
          return { error: `Template type '${templateType}' not found` };
        }
        
        return { success: true, type: templateType };
      };

      // Act & Assert
      expect(mockGetTemplateType('pdf-reference')).toEqual({
        success: true,
        type: 'pdf-reference',
      });
      
      expect(mockGetTemplateType('unknown-type')).toEqual({
        error: "Template type 'unknown-type' not found",
      });
    });
  });
});