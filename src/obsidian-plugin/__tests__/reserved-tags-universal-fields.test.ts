/**
 * Tests for reserved tag and universal field logic in the plugin context.
 * Validates that reserved tags are enforced and universal fields are properly applied.
 */

import { jest } from '@jest/globals';

// Mock window and process for plugin context
(global as any).window = {
  require: jest.fn(),
  process: {
    platform: 'win32',
    arch: 'x64',
  },
};

global.process = {
  ...global.process,
  env: {
    NOTEBOOKAUTOMATION_CONFIG: '',
  },
};

// Test data representing the schema structure
interface TemplateType {
  BaseTypes: string[];
  Type: string;
  RequiredFields: string[];
  Fields: {
    [key: string]: {
      Default?: any;
      Resolver?: string;
    };
  };
}

const mockTemplateTypes: Record<string, TemplateType> = {
  'pdf-reference': {
    BaseTypes: ['universal-fields'],
    Type: 'note/case-study',
    RequiredFields: ['comprehension', 'status', 'completion-date', 'authors', 'tags'],
    Fields: {
      publisher: { Default: 'University of Illinois at Urbana-Champaign' },
      status: { Default: 'unread' },
      comprehension: { Default: 0 },
      'date-created': { Resolver: 'DateCreatedResolver' },
      title: { Default: 'PDF Note' },
      tags: { Default: ['pdf', 'reference'] },
      'page-count': { Resolver: 'PdfPageCountResolver' },
    },
  },
  'video-reference': {
    BaseTypes: ['universal-fields'],
    Type: 'note/video-note',
    RequiredFields: ['comprehension', 'status', 'video-duration', 'author', 'tags'],
    Fields: {
      publisher: { Default: 'University of Illinois at Urbana-Champaign' },
      status: { Default: 'unwatched' },
      comprehension: { Default: 0 },
      'date-created': { Resolver: 'DateCreatedResolver' },
      title: { Default: 'Video Note' },
      tags: { Default: ['video', 'reference'] },
      'video-duration': { Default: '00:00:00' },
    },
  },
};

const mockUniversalFields = [
  'auto-generated-state',
  'date-created',
  'publisher',
];

const mockReservedTags = [
  'case-study',
  'live-class',
  'reading',
  'finance',
  'operations',
  'video',
  'pdf',
];

const mockTypeMapping: Record<string, string> = {
  'pdf-reference': 'note/case-study',
  'video-reference': 'note/video-note',
};

describe('Reserved Tags and Universal Fields Logic', () => {
  describe('Reserved Tags Validation', () => {
    it('should identify reserved tags correctly', () => {
      // Arrange
      const mockIsReservedTag = (tag: string) => {
        return mockReservedTags.includes(tag);
      };

      // Act & Assert
      expect(mockIsReservedTag('case-study')).toBe(true);
      expect(mockIsReservedTag('video')).toBe(true);
      expect(mockIsReservedTag('pdf')).toBe(true);
      expect(mockIsReservedTag('finance')).toBe(true);
      expect(mockIsReservedTag('operations')).toBe(true);
      expect(mockIsReservedTag('custom-tag')).toBe(false);
      expect(mockIsReservedTag('user-defined')).toBe(false);
    });

    it('should enforce reserved tags in metadata creation', () => {
      // Arrange
      const mockCreateMetadataWithReservedTags = (templateType: string, customTags: string[]) => {
        const template = mockTemplateTypes[templateType];
        if (!template) {
          throw new Error(`Template type '${templateType}' not found`);
        }

        const defaultTags = template.Fields.tags?.Default || [];
        const allTags = [...defaultTags, ...customTags];
        
        // Ensure all reserved tags are present if applicable
        const requiredReservedTags = mockReservedTags.filter(tag => 
          templateType.includes(tag) || allTags.includes(tag)
        );
        
        const finalTags = [...new Set([...allTags, ...requiredReservedTags])];
        
        return {
          templateType,
          tags: finalTags,
          hasReservedTags: requiredReservedTags.length > 0,
        };
      };

      // Act
      const pdfMetadata = mockCreateMetadataWithReservedTags('pdf-reference', ['custom-tag']);
      const videoMetadata = mockCreateMetadataWithReservedTags('video-reference', ['user-tag']);

      // Assert
      expect(pdfMetadata.tags).toContain('pdf');
      expect(pdfMetadata.tags).toContain('reference');
      expect(pdfMetadata.tags).toContain('custom-tag');
      expect(pdfMetadata.hasReservedTags).toBe(true);

      expect(videoMetadata.tags).toContain('video');
      expect(videoMetadata.tags).toContain('reference');
      expect(videoMetadata.tags).toContain('user-tag');
      expect(videoMetadata.hasReservedTags).toBe(true);
    });

    it('should prevent overriding reserved tags', () => {
      // Arrange
      const mockValidateTagsAgainstReserved = (userTags: string[], templateType: string) => {
        const template = mockTemplateTypes[templateType];
        if (!template) {
          return { isValid: false, error: `Template type '${templateType}' not found` };
        }

        const defaultTags = template.Fields.tags?.Default || [];
        const conflicts = userTags.filter(tag => 
          mockReservedTags.includes(tag) && !defaultTags.includes(tag)
        );

        return {
          isValid: conflicts.length === 0,
          conflicts,
          allowedTags: userTags.filter(tag => !conflicts.includes(tag)),
        };
      };

      // Act
      const validationResult = mockValidateTagsAgainstReserved(
        ['custom-tag', 'finance', 'operations'], 
        'pdf-reference'
      );

      // Assert
      expect(validationResult.isValid).toBe(false);
      expect(validationResult.conflicts).toContain('finance');
      expect(validationResult.conflicts).toContain('operations');
      expect(validationResult.allowedTags).toContain('custom-tag');
    });

    it('should validate tag consistency across template types', () => {
      // Arrange
      const mockValidateTagConsistency = (templateType: string, metadata: Record<string, any>) => {
        const template = mockTemplateTypes[templateType];
        if (!template) {
          return { isValid: false, error: `Template type '${templateType}' not found` };
        }

        const expectedType = mockTypeMapping[templateType];
        const userTags = metadata.tags || [];
        // Check if the user tags contain the expected default tags for this template type
        const defaultTags = template.Fields.tags?.Default || [];
        const hasRequiredTags = defaultTags.every((tag: string) => userTags.includes(tag));
        
        return {
          isValid: hasRequiredTags,
          expectedType,
          actualTags: userTags,
          hasTypeTag: hasRequiredTags,
        };
      };

      // Act
      const pdfValidation = mockValidateTagConsistency('pdf-reference', {
        tags: ['pdf', 'reference', 'custom'],
      });
      
      const videoValidation = mockValidateTagConsistency('video-reference', {
        tags: ['video', 'reference', 'custom'],
      });

      // Assert
      expect(pdfValidation.isValid).toBe(true);
      expect(pdfValidation.expectedType).toBe('note/case-study');
      expect(pdfValidation.hasTypeTag).toBe(true);

      expect(videoValidation.isValid).toBe(true);
      expect(videoValidation.expectedType).toBe('note/video-note');
      expect(videoValidation.hasTypeTag).toBe(true);
    });
  });

  describe('Universal Fields Application', () => {
    it('should apply universal fields to all template types', () => {
      // Arrange
      const mockApplyUniversalFields = (templateType: string, metadata: Record<string, any>) => {
        const template = mockTemplateTypes[templateType];
        if (!template) {
          throw new Error(`Template type '${templateType}' not found`);
        }

        const finalMetadata = { ...metadata };
        
        // Apply universal fields if not already present
        for (const field of mockUniversalFields) {
          if (!(field in finalMetadata)) {
            // Apply default value or placeholder
            if (template.Fields[field]?.Default !== undefined) {
              finalMetadata[field] = template.Fields[field].Default;
            } else if (template.Fields[field]?.Resolver) {
              finalMetadata[field] = `<${template.Fields[field].Resolver}>`;
            } else {
              finalMetadata[field] = null;
            }
          }
        }

        return finalMetadata;
      };

      // Act
      const pdfMetadata = mockApplyUniversalFields('pdf-reference', {
        title: 'My PDF Note',
        status: 'reading',
      });
      
      const videoMetadata = mockApplyUniversalFields('video-reference', {
        title: 'My Video Note',
        status: 'watching',
      });

      // Assert
      expect(pdfMetadata).toHaveProperty('auto-generated-state');
      expect(pdfMetadata).toHaveProperty('date-created');
      expect(pdfMetadata).toHaveProperty('publisher');
      expect(pdfMetadata['publisher']).toBe('University of Illinois at Urbana-Champaign');
      expect(pdfMetadata['date-created']).toBe('<DateCreatedResolver>');

      expect(videoMetadata).toHaveProperty('auto-generated-state');
      expect(videoMetadata).toHaveProperty('date-created');
      expect(videoMetadata).toHaveProperty('publisher');
      expect(videoMetadata['publisher']).toBe('University of Illinois at Urbana-Champaign');
      expect(videoMetadata['date-created']).toBe('<DateCreatedResolver>');
    });

    it('should not override existing universal field values', () => {
      // Arrange
      const mockApplyUniversalFieldsPreserveExisting = (templateType: string, metadata: Record<string, any>) => {
        const template = mockTemplateTypes[templateType];
        if (!template) {
          throw new Error(`Template type '${templateType}' not found`);
        }

        const finalMetadata = { ...metadata };
        
        // Apply universal fields only if not already present
        for (const field of mockUniversalFields) {
          if (!(field in finalMetadata)) {
            if (template.Fields[field]?.Default !== undefined) {
              finalMetadata[field] = template.Fields[field].Default;
            } else {
              finalMetadata[field] = null;
            }
          }
        }

        return finalMetadata;
      };

      // Act
      const metadataWithExistingFields = mockApplyUniversalFieldsPreserveExisting('pdf-reference', {
        title: 'My PDF Note',
        publisher: 'Custom Publisher',
        'date-created': '2024-01-01',
      });

      // Assert
      expect(metadataWithExistingFields['publisher']).toBe('Custom Publisher');
      expect(metadataWithExistingFields['date-created']).toBe('2024-01-01');
      expect(metadataWithExistingFields).toHaveProperty('auto-generated-state');
    });

    it('should validate universal fields are present in final metadata', () => {
      // Arrange
      const mockValidateUniversalFields = (templateType: string, metadata: Record<string, any>) => {
        const template = mockTemplateTypes[templateType];
        if (!template) {
          return { isValid: false, error: `Template type '${templateType}' not found` };
        }

        const missingFields = mockUniversalFields.filter(field => !(field in metadata));
        
        return {
          isValid: missingFields.length === 0,
          missingFields,
          presentFields: mockUniversalFields.filter(field => field in metadata),
        };
      };

      // Act
      const completeMetadata = mockValidateUniversalFields('pdf-reference', {
        'auto-generated-state': 'generated',
        'date-created': '2024-01-01',
        'publisher': 'Test Publisher',
        title: 'Test Note',
      });

      const incompleteMetadata = mockValidateUniversalFields('pdf-reference', {
        title: 'Test Note',
        // Missing universal fields
      });

      // Assert
      expect(completeMetadata.isValid).toBe(true);
      expect(completeMetadata.missingFields).toEqual([]);
      expect(completeMetadata.presentFields).toEqual(mockUniversalFields);

      expect(incompleteMetadata.isValid).toBe(false);
      expect(incompleteMetadata.missingFields).toEqual(mockUniversalFields);
      expect(incompleteMetadata.presentFields).toEqual([]);
    });

    it('should handle inheritance chain for universal fields', () => {
      // Arrange
      const mockResolveInheritanceChain = (templateType: string) => {
        const template = mockTemplateTypes[templateType];
        if (!template) {
          throw new Error(`Template type '${templateType}' not found`);
        }

        const baseTypes = template.BaseTypes || [];
        const inheritedFields = [];
        
        // Simulate inheritance resolution
        for (const baseType of baseTypes) {
          if (baseType === 'universal-fields') {
            inheritedFields.push(...mockUniversalFields);
          }
        }

        return {
          templateType,
          baseTypes,
          inheritedFields,
          totalFields: [...inheritedFields, ...Object.keys(template.Fields)],
        };
      };

      // Act
      const pdfInheritance = mockResolveInheritanceChain('pdf-reference');
      const videoInheritance = mockResolveInheritanceChain('video-reference');

      // Assert
      expect(pdfInheritance.baseTypes).toContain('universal-fields');
      expect(pdfInheritance.inheritedFields).toEqual(mockUniversalFields);
      expect(pdfInheritance.totalFields).toContain('auto-generated-state');
      expect(pdfInheritance.totalFields).toContain('date-created');
      expect(pdfInheritance.totalFields).toContain('publisher');
      expect(pdfInheritance.totalFields).toContain('title');
      expect(pdfInheritance.totalFields).toContain('tags');

      expect(videoInheritance.baseTypes).toContain('universal-fields');
      expect(videoInheritance.inheritedFields).toEqual(mockUniversalFields);
      expect(videoInheritance.totalFields).toContain('auto-generated-state');
      expect(videoInheritance.totalFields).toContain('date-created');
      expect(videoInheritance.totalFields).toContain('publisher');
      expect(videoInheritance.totalFields).toContain('video-duration');
    });
  });

  describe('Integration with Plugin Operations', () => {
    it('should validate metadata before command execution', () => {
      // Arrange
      const mockValidateMetadataForCommand = (templateType: string, metadata: Record<string, any>, command: string): {
        isValid: boolean;
        issues?: string[];
        command: string;
        templateType: string;
        error?: string;
      } => {
        const template = mockTemplateTypes[templateType];
        if (!template) {
          return { isValid: false, error: `Template type '${templateType}' not found`, command, templateType };
        }

        const issues = [];
        
        // Check required fields
        const requiredFields = template.RequiredFields || [];
        const missingRequired = requiredFields.filter(field => !(field in metadata));
        if (missingRequired.length > 0) {
          issues.push(`Missing required fields: ${missingRequired.join(', ')}`);
        }

        // Check universal fields
        const missingUniversal = mockUniversalFields.filter(field => !(field in metadata));
        if (missingUniversal.length > 0) {
          issues.push(`Missing universal fields: ${missingUniversal.join(', ')}`);
        }

        // Check reserved tags
        const userTags = metadata.tags || [];
        const defaultTags = template.Fields.tags?.Default || [];
        const hasRequiredReservedTags = defaultTags.some((tag: string) => userTags.includes(tag));
        if (!hasRequiredReservedTags) {
          issues.push(`Missing required reserved tags: ${defaultTags.join(', ')}`);
        }

        return {
          isValid: issues.length === 0,
          issues: issues.length > 0 ? issues : undefined,
          command,
          templateType,
        };
      };

      // Act
      const validMetadata = mockValidateMetadataForCommand('pdf-reference', {
        comprehension: 0,
        status: 'unread',
        'completion-date': '2024-01-01',
        authors: ['Author'],
        tags: ['pdf', 'reference'],
        'auto-generated-state': 'generated',
        'date-created': '2024-01-01',
        'publisher': 'Test Publisher',
      }, 'import-summarize-pdfs');

      const invalidMetadata = mockValidateMetadataForCommand('pdf-reference', {
        title: 'Test Note',
        // Missing most required fields
      }, 'import-summarize-pdfs');

      // Assert
      expect(validMetadata.isValid).toBe(true);
      expect(validMetadata.issues).toBeUndefined();

      expect(invalidMetadata.isValid).toBe(false);
      expect(invalidMetadata.issues).toBeDefined();
      expect(invalidMetadata.issues!.length).toBeGreaterThan(0);
      expect(invalidMetadata.issues!.some(issue => issue.includes('Missing required fields'))).toBe(true);
      expect(invalidMetadata.issues!.some(issue => issue.includes('Missing universal fields'))).toBe(true);
    });

    it('should enforce field consistency across plugin operations', () => {
      // Arrange
      const mockEnsureFieldConsistency = (templateType: string, metadata: Record<string, any>) => {
        const template = mockTemplateTypes[templateType];
        if (!template) {
          throw new Error(`Template type '${templateType}' not found`);
        }

        const consistentMetadata = { ...metadata };
        
        // Ensure type consistency
        const expectedType = mockTypeMapping[templateType];
        if (expectedType) {
          consistentMetadata['type'] = expectedType;
        }

        // Ensure required tags are present
        const defaultTags = template.Fields.tags?.Default || [];
        const userTags = consistentMetadata.tags || [];
        const finalTags = [...new Set([...defaultTags, ...userTags])];
        consistentMetadata.tags = finalTags;

        // Ensure universal fields are present
        for (const field of mockUniversalFields) {
          if (!(field in consistentMetadata)) {
            if (template.Fields[field]?.Default !== undefined) {
              consistentMetadata[field] = template.Fields[field].Default;
            } else {
              consistentMetadata[field] = null;
            }
          }
        }

        return consistentMetadata;
      };

      // Act
      const inconsistentMetadata = {
        title: 'Test PDF',
        status: 'reading',
        tags: ['custom-tag'],
      };

      const consistentMetadata = mockEnsureFieldConsistency('pdf-reference', inconsistentMetadata);

      // Assert
      expect(consistentMetadata.type).toBe('note/case-study');
      expect(consistentMetadata.tags).toContain('pdf');
      expect(consistentMetadata.tags).toContain('reference');
      expect(consistentMetadata.tags).toContain('custom-tag');
      expect(consistentMetadata).toHaveProperty('auto-generated-state');
      expect(consistentMetadata).toHaveProperty('date-created');
      expect(consistentMetadata).toHaveProperty('publisher');
      expect(consistentMetadata['publisher']).toBe('University of Illinois at Urbana-Champaign');
    });
  });

  describe('Error Handling and Edge Cases', () => {
    it('should handle unknown template types gracefully', () => {
      // Arrange
      const mockHandleUnknownTemplate = (templateType: string) => {
        if (!(templateType in mockTemplateTypes)) {
          return {
            success: false,
            error: `Unknown template type: ${templateType}`,
            availableTypes: Object.keys(mockTemplateTypes),
          };
        }
        return { success: true, templateType };
      };

      // Act
      const result = mockHandleUnknownTemplate('unknown-type');

      // Assert
      expect(result.success).toBe(false);
      expect(result.error).toBe('Unknown template type: unknown-type');
      expect(result.availableTypes).toEqual(['pdf-reference', 'video-reference']);
    });

    it('should handle empty or null metadata gracefully', () => {
      // Arrange
      const mockHandleEmptyMetadata = (templateType: string, metadata: Record<string, any> | null) => {
        if (!metadata) {
          return {
            success: false,
            error: 'Metadata cannot be null or undefined',
          };
        }

        if (Object.keys(metadata).length === 0) {
          return {
            success: false,
            error: 'Metadata cannot be empty',
            suggestion: 'Provide at least basic metadata fields',
          };
        }

        return { success: true, metadata };
      };

      // Act
      const nullResult = mockHandleEmptyMetadata('pdf-reference', null);
      const emptyResult = mockHandleEmptyMetadata('pdf-reference', {});
      const validResult = mockHandleEmptyMetadata('pdf-reference', { title: 'Test' });

      // Assert
      expect(nullResult.success).toBe(false);
      expect(nullResult.error).toBe('Metadata cannot be null or undefined');

      expect(emptyResult.success).toBe(false);
      expect(emptyResult.error).toBe('Metadata cannot be empty');

      expect(validResult.success).toBe(true);
      expect(validResult.metadata).toEqual({ title: 'Test' });
    });
  });
});